///-----------------------------------------------------------------
///   File:         IChunkSender.cs
///   Author:   	Andre Laskawy           
///   Date:         30.09.2018 16:19:00
///-----------------------------------------------------------------

namespace Nanomite.Core.Network.Common
{
    using Google.Protobuf;
    using Nanomite.Core.Network.Common;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="IChunkSender" />
    /// </summary>
    /// <typeparam name="C"></typeparam>
    /// <typeparam name="F"></typeparam>
    /// <typeparam name="R"></typeparam>
    public interface IChunkSender<C, F, R>
        where C : IMessage
        where F : IMessage
        where R : IMessage
    {
        /// <summary>
        /// Splits the binary file into chunks and send them in seperate streams to the grpc host.
        /// </summary>
        /// <param name="client">The actual client object.</param>
        /// <param name="cmd">The cmd<see cref="C"/></param>
        /// <param name="timeout">The timeout.</param>
        /// <returns>a task</returns>
        Task SendFile(IClient<C, F, R> client, C cmd, int timeout = 60);
    }
}
