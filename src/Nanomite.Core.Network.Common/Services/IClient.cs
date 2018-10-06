///-----------------------------------------------------------------
///   File:         IClient.cs
///   Author:   	Andre Laskawy           
///   Date:         30.09.2018 15:04:51
///-----------------------------------------------------------------

namespace Nanomite.Core.Network.Common
{
    using global::Grpc.Core;
    using Google.Protobuf;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="IClient{T}"/>
    /// </summary>
    /// <typeparam name="C"></typeparam>
    /// <typeparam name="F"></typeparam>
    /// <typeparam name="R"></typeparam>
    public interface IClient<C, F, R> : IDisposable
        where C : IMessage
        where F : IMessage
        where R : IMessage
    {
        /// <summary>
        /// Gets or sets the action that is called as soon a client is connected.
        /// </summary>
        Action OnConnected { get; set; }

        /// <summary>
        /// Gets or sets the action that is called as soon a client is disconnect.
        /// </summary>
        Action OnDisconnected { get; set; }

        /// <summary>
        /// Gets or sets the on action that is called when a notification is thrown.
        /// </summary>
        Action<object, string, NLog.LogLevel> OnNotify { get; set; }

        /// <summary>
        /// Gets or sets the function that is called when a command is received via stream channel.
        /// </summary>
        Action<C> OnStreaming { get; set; }

        /// <summary>
        /// Gets or sets the main stream object, which will be opened on the first connect.
        /// This stream is mainly used to realized the publish subcribe pattern.
        /// </summary>
        IStream<C> Stream { get; set; }

        /// <summary>
        /// The function will send a inital command via rpc to the cloud containing a S_ConnectionMessage proto with the user credentials.
        /// The password will be send as a salted MD5 Hash. This and each other call (stream and rpc) need to contain some header information:
        /// - Stream identifier
        /// - Version of the client
        /// - Token (can be null on initial connect message)
        /// If the users access is granted the cloud reponds the rpc result with a user object containg a jwt token, which has to be included
        /// inside the header on each streaming or rpc request.
        /// </summary>
        /// <param name="streamId">The stream identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="pass">The password of the user.</param>
        /// <param name="secretToken">The secret token.</param>
        /// <param name="optionalHeader">The optional header.</param>
        /// <param name="tryReconnect">if set to <c>true</c> [try reconnect].</param>
        /// <returns>
        /// A token/&gt;
        /// </returns>
        Task<string> Connect(string streamId, string userId, string pass, string secretToken, Metadata optionalHeader, bool tryReconnect = false);

        /// <summary>
        /// Opens a permanent stream.
        /// </summary>
        /// <param name="streamId">The stream identifier.</param>
        /// <param name="token">The token.</param>
        /// <param name="header">The header.</param>
        /// <param name="tryReconnect">if set to <c>true</c> [try reconnect].</param>
        /// <returns>a task</returns>
        Task OpenStream(string streamId, string token, Metadata header, bool tryReconnect);

        /// <summary>
        /// This function is used to request data from the cloud. To give the neccesary information about the data we want to receive
        /// the FetchRequest proto is used. The authentication token has to given also.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="token">The token.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns>a grpc response containg the result</returns>
        Task<R> Fetch(F request, string token, int timeout = 60);

        /// <summary>
        /// This function is used to send a command via rpc call. The command proto contains the specific data 
        /// that is requiered for the specific command key. The authentication token has to given also.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="token">The token.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns>a rpc response</returns>
        Task<R> Execute(C command, string token, int timeout = 60);

        /// <summary>
        /// This function is used to send a command via rpc call. The command proto contains the specific data
        /// that is requiered for the specific command key. The authentication token has to given also.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="token">The token.</param>
        /// <param name="streamId">The stream identifier.</param>
        /// <param name="header">The header.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns>
        /// a rpc response
        /// </returns>
        Task<R> ExecuteRaw(C command, string token, string streamId, Metadata header, int timeout = 60);

        /// <summary>
        /// This function is use to send a command via stream
        /// If you want to send large files (via U_File proto) this method need to be used.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="timeout">(Optional) The timeout is being used to define when a file transfer timed out.</param>
        /// <returns></returns>
        Task WriteOnStream(C command, int timeout = 60);

        /// <summary>
        /// This function is used to generate a new stream instance. E.g. to send file via multiple stream.
        /// USE THIS METHOD ONLY IF YOU KNOW WHAT YOU ARE DOING!
        /// </summary>
        /// <returns></returns>
        IStream<C> GetStream();
    }
}
