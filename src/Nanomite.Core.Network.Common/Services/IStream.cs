///-----------------------------------------------------------------
///   File:         IStream.cs
///   Author:   	Andre Laskawy           
///   Date:         30.09.2018 15:04:52
///-----------------------------------------------------------------

namespace Nanomite.Core.Network.Common
{
    using Google.Protobuf;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="IStream" />
    /// </summary>
    public interface IStream<C> where C : IMessage
    {
        /// <summary>
        /// Gets or sets the stream unique identifier
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// Gets or sets the Locked
        /// Gets or sets a value indicating whether the stream is locked 
        /// (currently sending a command)
        /// </summary>
        bool Locked { get; set; }

        /// <summary>
        /// Gets or sets the Connected
        /// Gets or sets a value indicating whether the client is connected
        /// </summary>
        bool Connected { get; set; }

        /// <summary>
        /// Writes the package asynchronous to the stream.
        /// It´s highly recommented to use the AddToQueue method instead.
        /// Use this method only in some special cirumstances.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The <see cref="Task"/></returns>
        Task WriteAsync(C data);

        /// <summary>
        /// Adds a package to the sending queue.
        /// </summary>
        /// <param name="package">The package.</param>
        void AddToQueue(C package);

        /// <summary>
        /// Starts the guard, which checking if the client is still connected and 
        /// which triggering the reconnect in a disconnect case.
        /// </summary>
        void StartGuard();

        /// <summary>
        /// Closes this instance.
        /// </summary>
        /// <returns>The <see cref="Task"/></returns>
        Task Close();
    }
}
