///-----------------------------------------------------------------
///   File:         GrpcServer.cs
///   Author:   	Andre Laskawy           
///   Date:         30.09.2018 16:24:13
///-----------------------------------------------------------------

namespace Nanomite.Common.Common.Services.GrpcService
{
    using global::Grpc.Core;
    using Nanomite.Services.Network.Common;
    using Nanomite.Services.Network.Common.Models;
    using Nanomite.Services.Network.Grpc;
    using Nanomite.Services.Network.Grpc.Models;
    using NLog;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="CommunicationServer"/>
    /// </summary>
    public class GRPCServer : GrpcServer.GrpcServerBase, IServer<Command, FetchRequest, GrpcResponse>
    {
        /// <summary>
        /// Creates the specific communication server
        /// </summary>
        /// <param name="host">The <see cref="IPEndPoint"/></param>
        /// <param name="chunkSender">The chunkSender<see cref="IChunkSender{Command, FetchRequest, GrpcResponse}"/></param>
        /// <param name="chunkReceiver">The chunkReceiver<see cref="IChunkReceiver{Command}"/></param>
        /// <returns>The <see cref="IServer{IMessage}"/></returns>
        public static IServer<Command, FetchRequest, GrpcResponse> Create(IPEndPoint host,
            IChunkSender<Command, FetchRequest, GrpcResponse> chunkSender,
            IChunkReceiver<Command> chunkReceiver)
        {
            return new GRPCServer(host, chunkSender, chunkReceiver);
        }

        /// <summary>
        /// The GRPC server
        /// </summary>
        private Server grpcServer = null;

        /// <summary>
        /// Responsible to receive chunked files
        /// </summary>
        private IChunkReceiver<Command> chunkReceiver = null;

        /// <summary>
        /// Responsible to send file via chunks
        /// </summary>
        private IChunkSender<Command, FetchRequest, GrpcResponse> chunkSender = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="GRPCServer"/> class.
        /// </summary>
        /// <param name="host">The <see cref="IPEndPoint"/></param>
        /// <param name="chunkSender">The chunkSender<see cref="IChunkSender{Command, FetchRequest, GrpcResponse}"/></param>
        /// <param name="chunkReceiver">The chunkReceiver<see cref="IChunkReceiver{Command}"/></param>
        internal GRPCServer(IPEndPoint host,
            IChunkSender<Command, FetchRequest, GrpcResponse> chunkSender,
            IChunkReceiver<Command> chunkReceiver)
        {
            this.Host = host;
            this.chunkSender = chunkSender;
            this.chunkReceiver = chunkReceiver;
            this.chunkReceiver.FileReceived = async (cmd, streamId, token, header) =>
            {
                // This creates a dummy stream only to receive the File and also because the main stream already exists in the subscription handler
                // The stream will be closed as soon as the file is being successfully received or a timeout happend
                IStream<Command> stream = new GrpcStream(null, null, streamId);
                (stream as GrpcStream).Connected = true;
                await this.OnStreaming?.Invoke(cmd, stream, token, header);
            };
        }

        /// <inheritdoc />
        public IPEndPoint Host { get; }

        /// <inheritdoc />
        public Action<string> OnClientDisconnected { get; set; }

        /// <inheritdoc />
        public Func<FetchRequest, string, string, Metadata, Task<GrpcResponse>> OnFetch { get; set; }

        /// <inheritdoc />
        public Func<Command, string, string, Metadata, Task<GrpcResponse>> OnCommand { get; set; }

        /// <inheritdoc />
        public Func<Command, IStream<Command>, string, Metadata, Task<GrpcResponse>> OnStreaming { get; set; }

        /// <inheritdoc />
        public Action<object, string, LogLevel> OnNotify { get; set; }

        /// <inheritdoc />
        public override async Task<GrpcResponse> Fetch(FetchRequest request, ServerCallContext context)
        {
            try
            {
                this.Log(this.ToString(), "fetching data...", LogLevel.Trace);
                if (!context.RequestHeaders.Any(p => p.Key == NetworkHeader.TokenHeader)
                    || !context.RequestHeaders.Any(p => p.Key == NetworkHeader.StreamIdHeader))
                {
                    throw new Exception("Missing header information");
                }

                string streamId = context.RequestHeaders.FirstOrDefault(p => p.Key == NetworkHeader.StreamIdHeader).Value;
                string token = context.RequestHeaders.FirstOrDefault(p => p.Key == NetworkHeader.TokenHeader).Value;

                var response = await this.OnFetch?.Invoke(request, streamId, token, context.RequestHeaders);
                this.Log(this.ToString(), "Send fetch response", LogLevel.Trace);
                return await Task.FromResult(response);
            }
            catch (Exception ex)
            {
                return new GrpcResponse() { Result = ResultCode.Error, Message = ex.ToText("Unknown fetch error") };
            }
        }

        /// <inheritdoc />
        public override async Task OpenStream(IAsyncStreamReader<Command> requestStream, IServerStreamWriter<Command> responseStream, ServerCallContext context)
        {
            try
            {
                while (await requestStream.MoveNext())
                {
                    this.Log(this.ToString(), "Received streaming package from client", LogLevel.Trace);
                    if (!context.RequestHeaders.Any(p => p.Key == NetworkHeader.TokenHeader)
                        || !context.RequestHeaders.Any(p => p.Key == NetworkHeader.StreamIdHeader))
                    {
                        throw new Exception("Missing header information");
                    }

                    string streamId = context.RequestHeaders.FirstOrDefault(p => p.Key == NetworkHeader.StreamIdHeader).Value;
                    string token = context.RequestHeaders.FirstOrDefault(p => p.Key == NetworkHeader.TokenHeader).Value;

                    try
                    {
                        if (requestStream.Current.Type == CommandType.File)
                        {
                            this.chunkReceiver.ChunkReceived(requestStream.Current, streamId, token, context.RequestHeaders);
                            continue;
                        }

                        IStream<Command> stream = new GrpcStream(responseStream, requestStream, streamId);
                        (stream as GrpcStream).Connected = true;
                        await this.OnStreaming?.Invoke(requestStream.Current, stream, token, context.RequestHeaders);
                    }
                    catch (Exception ex)
                    {
                        this.Log("Grpc stream error", ex);
                    }
                }
            }
            catch
            {
            }

            this.OnClientDisconnected?.Invoke(context.RequestHeaders.FirstOrDefault(p => p.Key == NetworkHeader.StreamIdHeader).Value);
            this.Log(this.ToString(), "Client connection closed", LogLevel.Info);
        }

        /// <inheritdoc />
        public override async Task<GrpcResponse> Execute(Command command, ServerCallContext context)
        {
            try
            {
                // Hearbeat from client
                if (command.Key == "Heartbeat")
                {
                    return new GrpcResponse() { Result = ResultCode.Ok };
                }

                this.Log(this.ToString(), "command data received...", LogLevel.Trace);
                if (!context.RequestHeaders.Any(p => p.Key == NetworkHeader.TokenHeader)
                    || !context.RequestHeaders.Any(p => p.Key == NetworkHeader.StreamIdHeader))
                {
                    throw new Exception("Missing header information");
                }

                string streamId = context.RequestHeaders.FirstOrDefault(p => p.Key == NetworkHeader.StreamIdHeader).Value;
                string token = context.RequestHeaders.FirstOrDefault(p => p.Key == NetworkHeader.TokenHeader).Value;

                var response = await this.OnCommand?.Invoke(command, streamId, token, context.RequestHeaders);
                this.Log(this.ToString(), "Send command response", LogLevel.Trace);
                return await Task.FromResult(response);
            }
            catch (Exception ex)
            {
                return new GrpcResponse() { Result = ResultCode.Error, Message = ex.ToText("Unknown fetch error") };
            }
        }

        /// <inheritdoc />
        public void Start()
        {
            try
            {
                ChannelOption optionReceive = new ChannelOption(ChannelOptions.MaxReceiveMessageLength, GrpcStream.MaxPackageSize);
                ChannelOption optionSend = new ChannelOption(ChannelOptions.MaxSendMessageLength, GrpcStream.MaxPackageSize);

                if (File.Exists("ca.crt"))
                {
                    var cacert = File.ReadAllText(@"ca.crt");
                    var servercert = File.ReadAllText(@"server.crt");
                    var serverkey = File.ReadAllText(@"server.key");
                    var keypair = new KeyCertificatePair(servercert, serverkey);
                    var sslCredentials = new SslServerCredentials(new List<KeyCertificatePair>() { keypair }, cacert, false);

                    this.grpcServer = new Server
                    {
                        Services = { GrpcServer.BindService(this) },
                        Ports = { new ServerPort(this.Host.Address.ToString(), this.Host.Port, sslCredentials) }
                    };
                }
                else
                {
                    this.grpcServer = new Server(new List<ChannelOption>() { optionReceive, optionSend })
                    {
                        Services = { GrpcServer.BindService(this) },
                        Ports = { new ServerPort(this.Host.Address.ToString(), this.Host.Port, ServerCredentials.Insecure) }
                    };
                }
                this.grpcServer.Start();

                this.Log(this.ToString(), "GRPC-Server server listening on port " + this.Host.Port, LogLevel.Info);
            }
            catch (Exception ex)
            {
                throw new Exception("Grpc server error", ex);
            }
        }

        /// <summary>
        /// The Dispose
        /// </summary>
        public void Dispose()
        {
            this.grpcServer.ShutdownAsync().Wait();
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
