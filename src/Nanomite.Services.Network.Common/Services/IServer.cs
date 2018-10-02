///-----------------------------------------------------------------
///   File:         IServer.cs
///   Author:   	Andre Laskawy           
///   Date:         30.09.2018 15:04:52
///-----------------------------------------------------------------

namespace Nanomite.Services.Network.Common
{
    using Google.Protobuf;
    using Grpc.Core;
    using System;
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="IServer" />
    /// </summary>
    /// <typeparam name="C"></typeparam>
    /// <typeparam name="F"></typeparam>
    /// <typeparam name="R"></typeparam>
    public interface IServer<C, F, R> : IDisposable
        where C : IMessage
        where F : IMessage
        where R : IMessage
    {
        /// <summary>
        /// Gets the host of the grpc stub.
        /// </summary>
        IPEndPoint Host { get; }

        /// <summary>
        /// Gets or sets the function that is called as soon a client disconnects.
        /// </summary>
        Action<string> OnClientDisconnected { get; set; }

        /// <summary>
        /// Gets or sets the function that is used to fetch data from the server via fetch request proto.
        /// </summary>
        Func<F, string, string, Metadata, Task<R>> OnFetch { get; set; }

        /// <summary>
        /// Gets or sets the OnCommand
        /// Gets or sets function that is called when a command is received via rpc call.
        /// Async receipt commands: During a command processing that requiers an answer, 
        /// the process helper will wait for the specific command with the correct id
        /// this command processing is handled inside this component and the receipt command wil not be thrown.
        /// </summary>
        Func<C, string, string, Metadata, Task<R>> OnCommand { get; set; }

        /// <summary>
        /// Gets or sets the function that is called when a command is received via stream channel.
        /// File chunking: large files are received inside the chunkreceiver and are handled inside the component inside. 
        /// The file will be thrown via OnCommand when all bytes are received.
        /// </summary>
        Func<C, IStream<C>, string, Metadata, Task<R>> OnStreaming { get; set; }

        /// <summary>
        /// Gets or sets the on action that is called when a notification is thrown.
        /// </summary>
        Action<object, string, NLog.LogLevel> OnNotify { get; set; }

        /// <summary>
        /// Starts the grpc server instance.
        /// </summary>
        void Start();
    }
}
