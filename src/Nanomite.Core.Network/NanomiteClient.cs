///-----------------------------------------------------------------
///   File:         NanomiteClient.cs
///   Author:   	Andre Laskawy           
///   Date:         03.10.2018 11:35:19
///-----------------------------------------------------------------

namespace Nanomite.Core.Network
{
    using global::Grpc.Core;
    using Google.Protobuf;
    using Google.Protobuf.WellKnownTypes;
    using Nanomite.Core.Network.Common;
    using Nanomite.Core.Network.Common.Chunking;
    using Nanomite.Core.Network.Common.Models;
    using Nanomite.Core.Network.Grpc;
    using NLog;
    using Semver;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="NanomiteClient" />
    /// </summary>
    public class NanomiteClient : IDisposable
    {
        /// <summary>
        /// Creates a GRPC client.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="clientId">The client identifier.</param>
        /// <returns>
        /// the nanomite client
        /// </returns>
        public static NanomiteClient CreateGrpcClient(string address, string clientId)
        {
            string version = FileVersionInfo.GetVersionInfo(Assembly.GetAssembly(typeof(GrpcClient)).Location).ProductVersion;
            var client = GrpcClient.Create(address, new ChunkSender(), new ChunkReceiver());
            CommunicationClient comClient = new CommunicationClient(client, clientId);
            return new NanomiteClient(comClient, version);
        }

        /// <summary>
        /// The client version
        /// </summary>
        private SemVersion clientVersion = null;

        /// <summary>
        /// The communication service wrapping the actual grpc client
        /// </summary>
        private CommunicationClient communicationClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="NanomiteClient"/> class.
        /// </summary>
        /// <param name="communicationClient">The communication service wrapping the actual grpc client/&gt;</param>
        /// <param name="clientVersion">The client version.</param>
        public NanomiteClient(CommunicationClient communicationClient, string clientVersion)
        {
            this.communicationClient = communicationClient;
            this.clientVersion = SemVersion.Parse(clientVersion.ToString());
            this.communicationClient.OnCommandReceived = (package, client) => { OnCommandReceived?.Invoke(package, client); };
            this.communicationClient.OnLog = (sender, srcId, msg, level) => { OnLog?.Invoke(sender, srcId, msg, level); };
            this.communicationClient.OnConnected = () => { OnConnected?.Invoke(); };
            this.communicationClient.OnDisconnected = () => { this.OnDisconnected?.Invoke(this.communicationClient.SrcDeviceId); };
        }

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
        public Action<string> OnDisconnected { get; set; }

        /// <summary>
        /// Gets or sets the action that is called when a log is thrown.
        /// </summary>
        public Action<object, string, string, LogLevel> OnLog { get; set; }

        /// <summary>
        /// Gets the unique identifier of the client.
        /// </summary>
        public string SrcDeviceId
        {
            get
            {
                return communicationClient.SrcDeviceId;
            }
        }

        /// <summary>
        /// Gets the current authentication token of the client,
        /// it is stored to this property after the connect automatically.
        /// </summary>
        public string Token { get; private set; }

        /// <summary>
        /// The function is used to connect to the cloud. After successful connection a permanent stream will be open
        /// to realize the publish subcribe pattern. The password will be send as a salted MD5 Hash.
        /// </summary>
        /// <param name="userName">The userName<see cref="string"/></param>
        /// <param name="password">The password<see cref="string"/></param>
        /// <param name="sercretToken">The sercretToken<see cref="string"/></param>
        /// <param name="tryReconnect">if set to <c>true</c> [try reconnect].</param>
        /// <returns>The <see cref="Task{string}"/></returns>
        public virtual async Task<string> ConnectAsync(string userName, string password, string sercretToken, bool tryReconnect = false)
        {
            try
            {
                Metadata header = new Metadata();
                header.Add(NetworkHeader.VersionHeader, this.clientVersion.ToString());

                this.Token = await this.communicationClient.Connect(sercretToken, userName, password, header, tryReconnect);
                return this.Token;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToText());
                throw;
            }
        }

        /// <summary>
        /// This function is used to request data from the cloud.
        /// The generic type is defining the IBaseModel type from that the proto type can be abstracted that need to be fetched.
        /// </summary>
        /// <typeparam name="T">any kind of type IBaseModel</typeparam>
        /// <param name="query">A custom odata query to filter the data./&gt;</param>
        /// <param name="includeAll">if set to <c>true</c> all child entities of the specific type will be included.</param>
        /// <returns>The <see cref="Task{List{T}}"/></returns>
        public async Task<List<T>> FetchData<T>(string query, bool includeAll = true) where T : IMessage, new()
        {
            try
            {
                if (this.communicationClient == null)
                {
                    throw new Exception("Client not initialized");
                }

                string odataQuery = string.Empty;
                if (query != null)
                {
                    odataQuery = query;
                }

                string typeUrl = Any.GetTypeName(Any.Pack((Activator.CreateInstance<T>() as IMessage)).TypeUrl);
                var response = await this.communicationClient.Fetch(typeUrl, odataQuery, includeAll);
                if (response.Result == ResultCode.Error)
                {
                    response.ToException(true);
                }

                return response.Data.Select(p => (T)p.CastToModel<T>()).ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToText());
                throw;
            }
        }

        /// <summary>
        /// The function is used to send an individual command to the cloud.
        /// </summary>
        /// <param name="cmd">The command.</param>
        /// <returns>The <see cref="Task{GrpcResponse}"/></returns>
        public async Task<GrpcResponse> SendCommandAsync(Command cmd)
        {
            try
            {
                if (this.communicationClient == null)
                {
                    throw new Exception("Client not initialized");
                }

                var response = await this.communicationClient.SendCommandAsync(cmd);
                if (response.Result == ResultCode.Error)
                {
                    response.ToException(true);
                }

                return response;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToText());
                throw;
            }
        }

        /// <summary>
        /// Sends the command asynchronous via stream. This method will not wait for a response from the server
        /// The command will be add to queue and send in a seperate thread to the cloud. (Fire and forget)
        /// </summary>
        /// <param name="cmd">The command.</param>
        /// <returns>a task</returns>
        public async Task SendCommandAsyncViaStream(Command cmd)
        {
            try
            {
                if (this.communicationClient == null)
                {
                    throw new Exception("Client not initialized");
                }

                var response = await this.communicationClient.SendCommandViaStreamAsync(cmd);
                if (response.Result == ResultCode.Error)
                {
                    response.ToException(true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToText());
                throw;
            }
        }

        /// <summary>
        /// The function is used to send a list of a speicifc proto type with a specific key to the cloud.
        /// A command will be generated automatically with the given data. The target id is only
        /// used to address a specific client.
        /// </summary>
        /// <typeparam name="T">any kind of a proto type</typeparam>
        /// <param name="list">The list of proto data.</param>
        /// <param name="topic">The publsh topic.</param>
        /// <param name="targetId">The target identifier.</param>
        /// <returns>The <see cref="Task{GrpcResponse}"/></returns>
        public async Task<GrpcResponse> SendCommandAsync<T>(List<T> list, string topic, string targetId = null) where T : IMessage
        {
            try
            {
                if (this.communicationClient == null)
                {
                    throw new Exception("Client not initialized");
                }

                var response = await this.communicationClient.SendCommandAsync(list, topic, targetId);
                if (response.Result == ResultCode.Error)
                {
                    response.ToException(true);
                }

                return response;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToText());
                throw;
            }
        }

        /// <summary>
        /// The function is used to send a proto with a specific key to the cloud.
        /// A command will be generated automatically with the given data. The target id is only
        /// used to address a specific client.
        /// </summary>
        /// <typeparam name="T">any kind of a proto type</typeparam>
        /// <param name="newValue">The proto object.</param>
        /// <param name="topic">The publsh topic.</param>
        /// <param name="targetId">The target identifier.</param>
        /// <returns>The <see cref="Task{GrpcResponse}"/></returns>
        public async Task<GrpcResponse> SendCommandAsync<T>(T newValue, string topic, string targetId = null) where T : IMessage
        {
            try
            {
                if (this.communicationClient == null)
                {
                    throw new Exception("Client not initialized");
                }

                var response = await this.communicationClient.SendCommandAsync<T>(newValue, topic, targetId);
                if (response.Result == ResultCode.Error)
                {
                    response.ToException(true);
                }

                return response;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToText());
                throw;
            }
        }

        /// <summary>
        /// Sends a file asynchronous. The file will be chunked automactically on client side 
        /// and restored on server side.
        /// The Chunksender and Chunkreceiver are used for this.
        /// </summary>
        /// <param name="fileProto">The file proto.</param>
        /// <returns>The <see cref="Task{GrpcResponse}"/></returns>
        public async Task<GrpcResponse> SendFileAsync(IMessage fileProto)
        {
            try
            {
                if (this.communicationClient == null)
                {
                    throw new Exception("Client not initialized");
                }

                Command cmd = new Command()
                {
                    Type = CommandType.File,
                    Topic = "FileTransfer"
                };

                cmd.Data.Add(Any.Pack(fileProto));

                var response = await this.communicationClient.SendCommandViaStreamAsync(cmd);
                if (response.Result == ResultCode.Error)
                {
                    response.ToException(true);
                }

                return response;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToText());
                throw;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            communicationClient.Dispose();
            Task.Delay(1000).Wait();
        }
    }
}
