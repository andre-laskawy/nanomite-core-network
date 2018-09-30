///-----------------------------------------------------------------
///   File:         IChunkReceiver.cs
///   Author:   	Andre Laskawy           
///   Date:         30.09.2018 16:19:00
///-----------------------------------------------------------------

namespace Nanomite.Services.Network.Common
{
    using Google.Protobuf;
    using Grpc.Core;
    using System;

    /// <summary>
    /// Defines the <see cref="IChunkReceiver" />
    /// </summary>
    /// <typeparam name="C"></typeparam>
    public interface IChunkReceiver<C> where C : IMessage
    {
        /// <summary>
        /// Gets or sets the action that is called after all chunks for file were received.
        /// </summary>
        Action<C, string, string, Metadata> FileReceived { get; set; }

        /// <summary>
        /// Handles a command of the type file. 
        /// It receives a chunk and builds a file out of it as soon all chunks are received.
        /// After a file is successfully build the event 'FileReceived' is being thrown.
        /// </summary>
        /// <param name="cmd">The command.</param>
        /// <param name="streamId">The stream identifier.</param>
        /// <param name="token">The token.</param>
        /// <param name="header">The header.</param>
        void ChunkReceived(C cmd, string streamId, string token, Metadata header);
    }
}
