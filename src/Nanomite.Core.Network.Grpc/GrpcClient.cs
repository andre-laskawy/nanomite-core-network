///-----------------------------------------------------------------
///   File:         GrpcClient.cs
///   Author:   	Andre Laskawy           
///   Date:         30.09.2018 16:24:13
///-----------------------------------------------------------------

namespace Nanomite.Core.Network.Grpc
{
    using global::Grpc.Core;
    using Google.Protobuf.WellKnownTypes;
    using Nanomite;
    using Nanomite.Common;
    using Nanomite.Core.Network.Common;
    using Nanomite.Core.Network.Common.Chunking;
    using Nanomite.Core.Network.Common.Models;
    using Nanomite.Core.Network.Grpc.Models;
    using NLog;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using static Nanomite.Core.Network.Common.GrpcServer;

    /// <summary>
    /// Defines the <see cref="GrpcClient" />
    /// </summary>
    public class GrpcClient : GrpcServerClient, IClient<Command, FetchRequest, GrpcResponse>
    {
        /// <summary>
        /// Creates the specific communication client based on grpc technoligy
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="chunkSender">The chunkSender<see cref="IChunkSender{Command, FetchRequest, GrpcResponse}"/></param>
        /// <param name="chunkReceiver">The chunkReceiver<see cref="IChunkReceiver{Command}"/></param>
        /// <returns>The <see cref="IClient{Command, FetchRequest, GrpcResponse}"/></returns>
        public static IClient<Command, FetchRequest, GrpcResponse> Create(string address, IChunkSender<Command, FetchRequest, GrpcResponse> chunkSender, IChunkReceiver<Command> chunkReceiver)
        {
            try
            {
                var endPoint = address.ToIpEndpoint();
                Channel channel = null;
                ChannelOption optionReceive = new ChannelOption(ChannelOptions.MaxReceiveMessageLength, GrpcStream.MaxPackageSize);
                ChannelOption optionSend = new ChannelOption(ChannelOptions.MaxSendMessageLength, GrpcStream.MaxPackageSize);

                if (File.Exists("ca.crt"))
                {
                    var cacert = File.ReadAllText(@"ca.crt");
                    var clientcert = File.ReadAllText(@"client.crt");
                    var clientkey = File.ReadAllText(@"client.key");
                    var ssl = new SslCredentials(cacert, new KeyCertificatePair(clientcert, clientkey));
                    channel = new Channel(string.Format("{0}:{1}", endPoint.Address.ToString(), endPoint.Port), ssl);
                }
                else
                {
                    channel = new Channel(string.Format("{0}:{1}", endPoint.Address.ToString(), endPoint.Port), ChannelCredentials.Insecure, new List<ChannelOption>() { optionReceive, optionSend });
                }

                return new GrpcClient(channel, endPoint, chunkSender, chunkReceiver);
            }
            catch (Exception ex)
            {
                throw new Exception("Grpc initialization error", ex);
            }
        }

        /// <summary>
        /// The header
        /// </summary>
        private Metadata header;

        /// <summary>
        /// The user
        /// </summary>
        private NetworkUser user;

        /// <summary>
        /// The secret token
        /// </summary>
        private string secretToken;

        /// <summary>
        /// The password
        /// </summary>
        private string password;

        /// <summary>
        /// Defines the
        /// </summary>
        private IPEndPoint remoteHost;

        /// <summary>
        /// The channel
        /// </summary>
        private Channel channel = null;

        /// <summary>
        /// Responsible to receive chunked files
        /// </summary>
        private IChunkReceiver<Command> chunkReceiver = null;

        /// <summary>
        /// Responsible to send file via chunks
        /// </summary>
        private IChunkSender<Command, FetchRequest, GrpcResponse> chunkSender = null;

        /// <summary>
        /// The stream object
        /// </summary>
        private AsyncDuplexStreamingCall<Command, Command> grpcStream = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="GrpcClient"/> class.
        /// </summary>
        /// <param name="channel">The channel to use to make remote calls.</param>
        /// <param name="host">The host<see cref="IPEndPoint"/></param>
        /// <param name="chunkSender">The chunkSender<see cref="IChunkSender{Command, FetchRequest, GrpcResponse}"/></param>
        /// <param name="chunkReceiver">The chunkReceiver<see cref="IChunkReceiver{Command}"/></param>
        internal GrpcClient(Channel channel,
            IPEndPoint host,
            IChunkSender<Command, FetchRequest, GrpcResponse> chunkSender = null,
            IChunkReceiver<Command> chunkReceiver = null) : base(channel)
        {
            this.channel = channel;
            this.remoteHost = host;
            this.chunkSender = chunkSender ?? new ChunkSender();
            this.chunkReceiver = chunkReceiver ?? new ChunkReceiver();
        }

        /// <inheritdoc />
        public IStream<Command> Stream { get; set; }

        /// <inheritdoc />
        public Action OnConnected { get; set; }

        /// <inheritdoc />
        public Action OnDisconnected { get; set; }

        /// <inheritdoc />
        public Action<object, string, LogLevel> OnNotify { get; set; }

        /// <inheritdoc />
        public Action<Command> OnStreaming { get; set; }

        /// <inheritdoc />
        public async Task<string> Connect(string streamId, string userId, string pass, string secretToken, Metadata optionalHeader, bool tryReconnect = false)
        {
            try
            {
                this.header = optionalHeader;
                this.secretToken = secretToken;
                this.password = pass;

                // clean up old stream smoothly
                if (this.Stream != null)
                {
                    await this.Stream.Close();
                    this.Stream = null;
                    this.grpcStream = null;
                }

                // Connect with user and get the token
                NetworkUser user = new NetworkUser()
                {
                    LoginName = userId,
                    PasswordHash = pass.Hash(secretToken)
                };

                Command loginCmd = new Command()
                {
                    Topic = StaticCommandKeys.Connect,
                    Type = CommandType.Action,
                    SenderId = streamId
                };
                loginCmd.Data.Add(Any.Pack(user));

                // connect
                var response = await this.ExecuteAsync(loginCmd, GetHeader(streamId, null, this.header));
                if (response.Result == ResultCode.Error)
                {
                    response.ToException(true);
                }

                this.user = response.Data.FirstOrDefault().CastToModel<NetworkUser>();

                // open stream
                await this.OpenStream(streamId, user.AuthenticationToken, this.header, tryReconnect);

                // return token
                return user.AuthenticationToken;
            }
            catch (Exception ex)
            {
                throw new Exception("Could not connect to cloud " + this.remoteHost.ToString() + " on port " + this.remoteHost.Port, ex);
            }
        }

        /// <inheritdoc />
        public async Task OpenStream(string streamId, string token, Metadata header, bool tryReconnect)
        {
            // establish new stream
            this.grpcStream = base.OpenStream(GetHeader(streamId, token, header));
            await this.grpcStream.RequestStream.WriteAsync(new Command() { Topic = StaticCommandKeys.OpenStream });
            this.Stream = new GrpcStream(grpcStream.RequestStream,
                grpcStream.ResponseStream,
                streamId,
                (message, timeout) => { base.Execute(message as Command, null, DateTime.UtcNow.AddSeconds(timeout)); });

            // register guard events
            (this.Stream as GrpcStream).Guard.EstablishConnection = () =>
            {
                this.StartReceiving();
                (this.Stream as GrpcStream).Connected = true;
                this.OnConnected?.Invoke();
            };

            (this.Stream as GrpcStream).Guard.OnConnectionLost = async (stream) =>
            {
                if (tryReconnect)
                {
                    await this.TryReconnect();
                }
            };

            // start the connection guard to observe the connection
            this.Stream.StartGuard();
        }

        /// <inheritdoc />
        public async Task<GrpcResponse> Fetch(FetchRequest request, string token, int timeout = 60)
        {
            try
            {
                this.Log(this.ToString(), "start fetching data over grpc", LogLevel.Trace);
                var result = await this.FetchAsync(request, GetHeader(this.Stream.Id, token, this.header), DateTime.UtcNow.AddSeconds(timeout));
                this.Log(this.ToString(), "fetch package received", LogLevel.Trace);
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Grpc fetch error", ex);
            }
        }

        /// <inheritdoc />
        public async Task<GrpcResponse> Execute(Command command, string token, int timeout = 60)
        {
            return await this.ExecuteRaw(command, token, this.Stream.Id, this.header, timeout);
        }

        /// <inheritdoc />
        public async Task<GrpcResponse> ExecuteRaw(Command command, string token, string streamId, Metadata header, int timeout = 60)
        {
            try
            {
                this.Log(this.ToString(), "Start send command", LogLevel.Trace);
                var result = await this.ExecuteAsync(command, GetHeader(streamId, token, header), DateTime.UtcNow.AddSeconds(timeout));
                this.Log(this.ToString(), "command result received", LogLevel.Trace);
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Grpc command error", ex);
            }
        }

        /// <inheritdoc />
        public IStream<Command> GetStream()
        {
            var grpcStream = base.OpenStream(GetHeader(this.Stream.Id, user.AuthenticationToken, this.header));
            return new GrpcStream(grpcStream.RequestStream, grpcStream.ResponseStream, this.Stream.Id, (message, timeout) => { base.Execute(message as Command, null, DateTime.UtcNow.AddSeconds(timeout)); });
        }

        /// <inheritdoc />
        public async Task WriteOnStream(Command command, int timeout = 60)
        {
            try
            {
                if (command.Type == CommandType.File)
                {
                    this.Log(this.ToString(), "Start send file", LogLevel.Debug);
                    await this.chunkSender.SendFile(this, command, timeout);
                }
                else
                {
                    this.Log(this.ToString(), "Start send command via stream", LogLevel.Debug);
                    this.Stream.AddToQueue(command);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Grpc stream command error", ex);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Stream.Close();
        }

        /// <summary>
        /// Starts the receiving.
        /// </summary>
        private void StartReceiving()
        {
            Task.Run(async () =>
            {
                try
                {
                    this.Log(this.ToString(), "Open grpc stream to server...", LogLevel.Trace);
                    (this.Stream as GrpcStream).Connected = true;
                    while (await (this.Stream as GrpcStream).StreamReader.MoveNext())
                    {
                        var package = (this.Stream as GrpcStream).StreamReader.Current;
                        this.Log(this.ToString(), "Streaming package received", LogLevel.Trace);
                        this.OnStreaming.Invoke(package);
                    }
                }
                catch
                { }

                (this.Stream as GrpcStream).Connected = false;
                this.OnDisconnected?.Invoke();
                Log(this.ToString(), "Connection closed", LogLevel.Info);
            });
        }

        /// <summary>
        /// Tries the reconnect.
        /// </summary>
        /// <returns>The <see cref="Task"/></returns>
        private async Task TryReconnect()
        {
            try
            {
                this.Log(this.ToString(), "Try reconnect", LogLevel.Debug);
                await this.Connect(this.Stream.Id, this.user.LoginName, this.password, this.secretToken, this.header, true);
            }
            catch
            {
                this.Log(this.ToString(), "Reconnect failed", LogLevel.Trace);
            }
        }

        /// <summary>
        /// Gets the header.
        /// </summary>
        /// <param name="streamId">The stream identifier.</param>
        /// <param name="token">The token.</param>
        /// <param name="optionalHeader">The optionalHeader<see cref="Metadata"/></param>
        /// <returns></returns>
        private Metadata GetHeader(string streamId, string token, Metadata optionalHeader)
        {
            Metadata header = new Metadata();
            header.Add(NetworkHeader.StreamIdHeader, streamId);
            header.Add(NetworkHeader.TokenHeader, token ?? string.Empty);
            foreach (var adHeader in optionalHeader)
            {
                header.Add(adHeader);
            }
            return header;
        }

        /// <summary>
        /// Logs the specified code.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="ex">The ex.</param>
        private void Log(string code, Exception ex)
        {
            this.Log(ex.Source, ex.ToText(code), LogLevel.Error);
        }

        /// <summary>
        /// Logs the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="msg">The MSG.</param>
        /// <param name="level">The level.</param>
        private void Log(string source, string msg, LogLevel level)
        {
            this.OnNotify?.Invoke(source, msg, level);
        }
    }
}
