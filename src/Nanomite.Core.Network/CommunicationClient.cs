///-----------------------------------------------------------------
///   File:         CommunicationClient.cs
///   Author:   	Andre Laskawy           
///   Date:         01.10.2018 20:20:52
///-----------------------------------------------------------------

namespace Nanomite.Core.Network
{
    using global::Grpc.Core;
    using Google.Protobuf;
    using Google.Protobuf.WellKnownTypes;
    using Nanomite.Core.Network.Common;
    using NLog;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="CommunicationClient"/>
    /// </summary>
    public class CommunicationClient
    {
        /// <summary>
        /// Initializes static members of the <see cref="CommunicationClient"/> class.
        /// </summary>
        static CommunicationClient()
        {
            ReceiptTimeout = 60;
            FetchTimeout = 60;
            StreamTimeout = int.MaxValue;
        }

        /// <summary>
        /// Gets or sets the fetch timeout (default is 60 seconds).
        /// </summary>
        public static int FetchTimeout { get; set; }

        /// <summary>
        /// Gets or sets the receipt timeout (default is 60 seconds).
        /// </summary>
        public static int ReceiptTimeout { get; set; }

        /// <summary>
        /// Gets or sets the stream timeout (default is infinity).
        /// </summary>
        public static int StreamTimeout { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        private string token = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommunicationClient"/> class.
        /// </summary>
        /// <param name="client">Accepts an client type that is implenting the IClient interface.</param>
        /// <param name="srcDeviceId">The unique identifier of the client</param>
        public CommunicationClient(IClient<Command, FetchRequest, GrpcResponse> client, string srcDeviceId)
        {
            this.IsOnline = false;
            this.SrcDeviceId = srcDeviceId;
            this.Client = client;

            this.Client.OnNotify += (sender, message, level) => { Log(sender.ToString(), this.SrcDeviceId, message, level); };
            this.Client.OnStreaming = (message) => this.DataReceived(message);
            this.Client.OnDisconnected = () =>
            {
                this.IsOnline = false;
                this.OnDisconnected?.Invoke();
            };
            this.Client.OnConnected = () =>
            {
                this.IsOnline = true;
                this.OnConnected?.Invoke();
            };
        }

        /// <summary>
        /// Gets the IClient object.
        /// </summary>
        public IClient<Command, FetchRequest, GrpcResponse> Client { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether IsOnline
        /// Gets a value indicating whether the client is online
        /// </summary>
        public bool IsOnline { get; set; }

        /// <summary>
        /// Gets the unique identifier of the client.
        /// </summary>
        public string SrcDeviceId { get; private set; }

        /// <summary>
        /// Gets or sets the action that is called when a command is received on the stream channel.
        /// </summary>
        public Action<Command, IClient<Command, FetchRequest, GrpcResponse>> OnCommandReceived { get; set; }

        /// <summary>
        /// Gets or sets the action that is called when the client is connected
        /// </summary>
        public Action OnConnected { get; set; }

        /// <summary>
        /// Gets or sets the action that is called when the client is disconnected
        /// </summary>
        public Action OnDisconnected { get; set; }

        /// <summary>
        /// Gets or sets the action that is called when a log is thrown.
        /// </summary>
        public Action<object, string, string, LogLevel> OnLog { get; set; }

        /// <summary>
        /// This function is used to request data from the cloud. 
        /// To give the neccesary information about the data the FetchRequest proto is used.
        /// </summary>
        /// <param name="requestedType">The specific type of the proto that need to be fetched (defined by the proto url).</param>
        /// <param name="query">A custom odata query to filter the data.</param>
        /// <param name="includeAll">if set to <c>true</c> all child entities of the specific type will be included.</param>
        /// <returns>The <see cref="Task{GrpcResponse}"/></returns>
        public async Task<GrpcResponse> Fetch(string requestedType, string query, bool includeAll)
        {
            this.Log(this, this.Client.Stream?.Id, "Fetching package", LogLevel.Debug);
            try
            {
                FetchRequest request = new FetchRequest()
                {
                    InlcudeRelatedEntities = includeAll,
                    Query = query,
                };

                request.TypeDescription = requestedType;
                return await this.Client.Fetch(request, this.token, FetchTimeout);
            }
            catch (Exception ex)
            {
                this.Log(this.SrcDeviceId, "Fetch error", ex);
                return new GrpcResponse() { Result = ResultCode.Error, Message = ex.ToText("Fetch error") };
            }
        }

        /// <summary>
        /// The function is used to connect to the cloud. After successful connection a permanent stream will be open
        /// to realize the publish subcribe pattern. The password will be send as a salted MD5 Hash.
        /// </summary>
        /// <param name="secretToken">The secretToken<see cref="string"/></param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="password">The password<see cref="string"/></param>
        /// <param name="header">Some optional header information (e.g. the version information of the client).</param>
        /// <param name="tryReconnect">if set to <c>true</c> [try reconnect].</param>
        /// <returns>the user specific JWT token</returns>
        public async Task<string> Connect(string secretToken, string userId, string password, Metadata header, bool tryReconnect = false)
        {
            this.token = await this.Client.Connect(this.SrcDeviceId, userId, password, secretToken, header, tryReconnect);
            await new Func<bool>(() => this.Client.Stream.Connected).AwaitResult(10);
            return this.token;
        }

        /// <summary>
        /// The function is used to send a proto with a specific key to the cloud.
        /// A command will be generated automatically with the given data. The topic is used to set the publisher topic.
        /// </summary>
        /// <typeparam name="G">any kind of a proto type</typeparam>
        /// <param name="data">The data.</param>
        /// <param name="key">The command key.</param>
        /// <param name="target">The target.</param>
        /// <returns>The <see cref="Task{GrpcResponse}"/></returns>
        public async Task<GrpcResponse> SendCommandAsync<G>(G data, string key, string target = null) where G : IMessage
        {
            if (this.IsOnline)
            {
                this.Log(this, this.SrcDeviceId, "Sending command...", NLog.LogLevel.Debug);
                Command cmd = new Command()
                {
                    Type = CommandType.Action,
                    Topic = key.ToString(),
                    SenderId = this.SrcDeviceId,
                    TargetId = target ?? ""
                };

                cmd.Data.Add(Any.Pack(data));
                return await this.Client.Execute(cmd, this.token, ReceiptTimeout);
            }
            else
            {
                return new GrpcResponse() { Result = ResultCode.Error, Message = "Client is offline" };
            }
        }

        /// <summary>
        /// The function is used to send a list of a speicifc proto type with a specific key to the cloud.
        /// A command will be generated automatically with the given data. The topic is used to set the publisher topic.
        /// </summary>
        /// <typeparam name="G">any kind of a proto type</typeparam>
        /// <param name="data">The data.</param>
        /// <param name="key">The command key.</param>
        /// <param name="target">The target.</param>
        /// <returns>The <see cref="Task{GrpcResponse}"/></returns>
        public async Task<GrpcResponse> SendCommandAsync<G>(List<G> data, string key, string target = null) where G : IMessage
        {
            if (this.IsOnline)
            {
                this.Log(this, this.SrcDeviceId, "Sending command...", NLog.LogLevel.Debug);
                Command cmd = new Command()
                {
                    Type = CommandType.Action,
                    Topic = key.ToString(),
                    SenderId = this.SrcDeviceId,
                    TargetId = target ?? ""
                };

                foreach (var item in data)
                {
                    cmd.Data.Add(Any.Pack(item));
                }

                return await this.Client.Execute(cmd, this.token, ReceiptTimeout);
            }
            else
            {
                return new GrpcResponse() { Result = ResultCode.Error, Message = "Client is offline" };
            }
        }

        /// <summary>
        /// The function is used to send an individual command to the cloud.
        /// </summary>
        /// <param name="cmd">The cmd<see cref="Command"/></param>
        /// <returns>The <see cref="Task{GrpcResponse}"/></returns>
        public async Task<GrpcResponse> SendCommandAsync(Command cmd)
        {
            if (this.IsOnline)
            {
                cmd.SenderId = this.SrcDeviceId;
                this.Log(this, this.SrcDeviceId, "Sending command...", NLog.LogLevel.Debug);
                return await this.Client.Execute(cmd, this.token, ReceiptTimeout);
            }
            else
            {
                return new GrpcResponse() { Result = ResultCode.Error, Message = "Client is offline" };
            }
        }

        /// <summary>
        /// The function is used to send an individual command via stream channel.
        /// This function can be used to send a big file or other big data via streaming.
        /// Attention: Be aware that the grpc response is not the response from the server, its just an indication that everything
        /// went well on the client side. The actual sending and receiving of the command(s) will be asynchronous.
        /// </summary>
        /// <param name="cmd">The cmd<see cref="Command"/></param>
        /// <returns>The <see cref="Task{GrpcResponse}"/></returns>
        public async Task<GrpcResponse> SendCommandViaStreamAsync(Command cmd)
        {
            if (this.IsOnline)
            {
                cmd.SenderId = this.SrcDeviceId;
                this.Log(this, this.SrcDeviceId, "Sending command via stream...", NLog.LogLevel.Debug);
                await this.Client.WriteOnStream(cmd, StreamTimeout);
                return new GrpcResponse() { Result = ResultCode.Ok };
            }
            else
            {
                return new GrpcResponse() { Result = ResultCode.Error, Message = "Client is offline" };
            }
        }

        /// <summary>
        /// Führt anwendungsspezifische Aufgaben durch, die mit der Freigabe, der Zurückgabe oder dem
        /// Zurücksetzen von nicht verwalteten Ressourcen zusammenhängen.
        /// </summary>
        public void Dispose()
        {
            this.Client.Dispose();
        }

        /// <summary>
        /// Is called when a command is received
        /// </summary>
        /// <param name="command">The command.</param>
        private async void DataReceived(Command command)
        {
            try
            {
                if (!await AsyncCommandProcessor.Notify(command))
                {
                    if (command.Type == CommandType.Action)
                    {
                        this.OnCommandReceived?.Invoke(command, this.Client);
                    }
                }
            }
            catch (Exception ex)
            {
                await Task.Delay(1);
                this.Log(this.SrcDeviceId, "Command process exception", ex);
            }
        }

        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="srcId">The source identifier.</param>
        /// <param name="msg">The MSG.</param>
        /// <param name="level">The level.</param>
        private void Log(object sender, string srcId, string msg, NLog.LogLevel level)
        {
            this.OnLog?.Invoke(sender, srcId, msg, level);
        }

        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="srcId">The source identifier.</param>
        /// <param name="code">The code.</param>
        /// <param name="ex">The ex.</param>
        private void Log(string srcId, string code, Exception ex)
        {
            string message = code + ":" + ex.Message + Environment.NewLine + ex.StackTrace;
            this.Log(ex.Source, srcId, message, NLog.LogLevel.Error);
        }
    }
}
