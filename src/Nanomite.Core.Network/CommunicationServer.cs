///-----------------------------------------------------------------
///   File:         CommunicationServer.cs
///   Author:   	Andre Laskawy           
///   Date:         01.10.2018 20:28:39
///-----------------------------------------------------------------

namespace Nanomite.Core.Network
{
    using global::Grpc.Core;
    using Nanomite.Core.Network.Common;
    using Nanomite.Core.Network.Common.Models;
    using NLog;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// A wrapper class around the <c>grpc</c> server instance
    /// </summary>
    public class CommunicationServer : IDisposable
    {
        /// <summary>
        /// The GRPC server
        /// </summary>
        private IServer<Command, FetchRequest, GrpcResponse> communicationService = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommunicationServer"/> class.
        /// </summary>
        /// <param name="communicationService">Accepts an server type that is implenting the IServer interface.</param>
        /// <param name="srcDeviceId">The unique identifier of the client</param>
        public CommunicationServer(IServer<Command, FetchRequest, GrpcResponse> communicationService, string srcDeviceId)
        {
            this.SrcDeviceId = srcDeviceId;
            this.communicationService = communicationService;

            this.communicationService.OnNotify += (sender, message, level) =>
            {
                Log(sender, this.SrcDeviceId, message, level);
            };
            this.communicationService.OnCommand = async (command, streamId, token, header) => { return await this.Action(command, streamId, token, header); };
            this.communicationService.OnFetch = async (request, streamId, token, header) => { return await this.Fetch(request, streamId, token, header); };
            this.communicationService.OnStreaming = async (cmd, stream, token, header) => { return await this.Streaming(cmd, stream, token, header); };
            this.communicationService.OnClientDisconnected = (id) => { this.OnClientDisconnected?.Invoke(id); };
        }

        /// <summary>
        /// Gets or sets the action that is called when a client is disconnected
        /// </summary>
        public Action<string> OnClientDisconnected { get; set; }

        /// <summary>
        /// Gets or sets the function that is called when a client wants to open a permanent grpc stream
        /// </summary>
        public Func<IStream<Command>, string, Metadata, Task<GrpcResponse>> OnStreamOpened { get; set; }

        /// <summary>
        /// Gets or sets the function that is called when a client send a command on a stream channel
        /// (usally this should be pretty raw, because most of the commands should be handled via rpc)
        /// </summary>
        public Func<Command, IStream<Command>, string, Metadata, Task<GrpcResponse>> OnStreaming { get; set; }

        /// <summary>
        /// Gets or sets the function that is used to process a fetch request
        /// </summary>
        public Func<FetchRequest, string, string, Metadata, Task<GrpcResponse>> OnFetch { get; set; }

        /// <summary>
        /// Gets or sets the OnAction
        /// Gets or sets function that is called when a command is received via rpc -> we call it action
        /// </summary>
        public Func<Command, string, string, Metadata, Task<GrpcResponse>> OnAction { get; set; }

        /// <summary>
        /// Gets or sets the action that is called to log a message
        /// </summary>
        public Action<object, string, string, LogLevel> OnLog { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the server.
        /// </summary>
        public string SrcDeviceId { get; set; }

        /// <summary>
        /// Starts the communication host instance which is defined by the IServer object.
        /// </summary>
        public void Start()
        {
            this.communicationService.Start();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.communicationService.Dispose();
        }

        /// <summary>
        /// Fetches the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="streamId">The streamId<see cref="string"/></param>
        /// <param name="token">The token<see cref="string"/></param>
        /// <param name="header">The header<see cref="Metadata"/></param>
        /// <returns>The <see cref="Task{IMessage}"/></returns>
        private async Task<GrpcResponse> Fetch(FetchRequest message, string streamId, string token, Metadata header)
        {
            if (string.IsNullOrEmpty(message.TypeDescription))
            {
                throw new Exception("no type descsriptor found inside the fetch request.");
            }

            if (string.IsNullOrEmpty(token))
            {
                throw new Exception("no token provided.");
            }

            // Fetch data
            return await OnFetch?.Invoke(message, streamId, token, header);
        }

        /// <summary>
        /// Sends and receive data from/to other clients
        /// </summary>
        /// <param name="command">The request message</param>
        /// <param name="streamId">The streamId<see cref="string"/></param>
        /// <param name="token">The token<see cref="string"/></param>
        /// <param name="header">The header<see cref="Metadata"/></param>
        /// <returns>The <see cref="Task"/></returns>
        private async Task<GrpcResponse> Action(Command command, string streamId, string token, Metadata header)
        {
            try
            {
                if (string.IsNullOrEmpty(token) 
                    && command.Topic != StaticCommandKeys.Connect)
                {
                    throw new Exception("no token provided.");
                }

                if (!await AsyncCommandProcessor.Notify(command))
                {
                    if (command.Type == CommandType.Action)
                    {
                        this.Log(this, this.SrcDeviceId, "Command received", LogLevel.Trace);
                        return await this.OnAction?.Invoke(command, streamId, token, header);
                    }

                    return new GrpcResponse() { Result = ResultCode.Error, Message = "Unknown type" };
                }
                else
                {
                    return new GrpcResponse() { Result = ResultCode.Ok };
                }
            }
            catch (Exception ex)
            {
                this.Log("", "Receiving error", ex);
                return new GrpcResponse() { Result = ResultCode.Error, Message = ex.ToText("Unknown action error") };
            }
        }

        /// <summary>
        /// Is called when a client wants to open a stream
        /// </summary>
        /// <param name="cmd">The command.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="token">The token.</param>
        /// <param name="header">The header.</param>
        /// <returns>
        /// The <see cref="Task{GrpcResponse}" /></returns>
        private async Task<GrpcResponse> Streaming(Command cmd, IStream<Command> stream, string token, Metadata header)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    throw new Exception("no token provided.");
                }

                if (cmd.Topic == StaticCommandKeys.OpenStream)
                {
                    return await OnStreamOpened?.Invoke(stream, token, header);
                }
                else
                {
                    return await this.OnStreaming?.Invoke(cmd, stream, token, header);
                }
            }
            catch (Exception ex)
            {
                this.Log("", "stream error", ex);
                return new GrpcResponse() { Result = ResultCode.Error, Message = ex.ToText("Stream error") };
            }
        }

        /// <summary>
        /// Logs the specified sender.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="scrId">The SCR identifier.</param>
        /// <param name="msg">The MSG.</param>
        /// <param name="level">The level.</param>
        private void Log(object sender, string scrId, string msg, LogLevel level)
        {
            this.OnLog?.Invoke(sender, scrId, msg, level);
        }

        /// <summary>
        /// Logs the specified source identifier.
        /// </summary>
        /// <param name="srcId">The source identifier.</param>
        /// <param name="code">The code.</param>
        /// <param name="ex">The ex.</param>
        private void Log(string srcId, string code, Exception ex)
        {
            string message = code + ":" + ex.Message + Environment.NewLine + ex.StackTrace;
            this.Log(ex.Source, srcId, message, LogLevel.Error);
        }
    }
}
