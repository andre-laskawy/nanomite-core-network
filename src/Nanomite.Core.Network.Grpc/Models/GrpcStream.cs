///-----------------------------------------------------------------
///   File:         GrpcStream.cs
///   Author:   	Andre Laskawy           
///   Date:         30.09.2018 16:24:13
///-----------------------------------------------------------------

namespace Nanomite.Core.Network.Grpc.Models
{
    using global::Grpc.Core;
    using Nanomite.Core.Network.Common;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="GrpcStream"/>
    /// </summary>
    public class GrpcStream : IStream<Command>
    {
        /// <summary>
        /// The maximum package size
        /// </summary>
        public static int MaxPackageSize = 32 * 1024 * 1024;

        /// <summary>
        /// Defines the streamQueueWorker
        /// </summary>
        private StreamQueueWorker<Command> streamQueueWorker;

        /// <summary>
        /// Initializes a new instance of the <see cref="GrpcStream"/> class.
        /// </summary>
        /// <param name="streamWriter">The stream writer.</param>
        /// <param name="streamReader">The stream reader.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="heartbead">The heartbead method.</param>
        public GrpcStream(dynamic streamWriter, IAsyncStreamReader<Command> streamReader, string id, Action<Command, int> heartbead = null)
        {
            this.Id = id;
            this.StreamWriter = streamWriter;
            this.StreamReader = streamReader;
            this.Locked = false;
            this.Guard = new GrpcGuard(heartbead);
            this.streamQueueWorker = new StreamQueueWorker<Command>(this);
        }

        /// <summary>
        /// Gets or sets the guard.
        /// </summary>
        /// <value>The guard.</value>
        /// <inheritdoc/>
        public GrpcGuard Guard { get; set; }

        /// <summary>
        /// Gets or sets the stream reader.
        /// </summary>
        internal IAsyncStreamReader<Command> StreamReader { get; set; }

        /// <summary>
        /// Gets or sets the stream writer.
        /// </summary>
        internal dynamic StreamWriter { get; set; }

        /// <inheritdoc />
        public bool Locked { get; set; }

        /// <inheritdoc />
        public string Id { get; set; }

        /// <inheritdoc />
        public bool Connected { get; set; }

        /// <inheritdoc />
        public async Task WriteAsync(Command package)
        {
            try
            {
                this.Locked = true;
                if (this.StreamWriter is IClientStreamWriter<Command>)
                {
                    await (this.StreamWriter as IClientStreamWriter<Command>).WriteAsync(package);
                }
                else
                {
                    await (this.StreamWriter as IServerStreamWriter<Command>).WriteAsync(package);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Message send/streaming error", ex);
            }
            finally
            {
                this.Locked = false;
            }
        }

        /// <inheritdoc />
        public void AddToQueue(Command package)
        {
            this.streamQueueWorker.Send(package);
        }

        /// <inheritdoc />
        public async Task Close()
        {
            try
            {
                if (this.StreamWriter is IClientStreamWriter<Command>)
                {
                    await (this.StreamWriter as IClientStreamWriter<Command>).CompleteAsync();
                }
            }
            catch
            { }

            try
            {
                if (this.StreamReader != null)
                {
                    this.StreamReader.Dispose();
                }
            }
            catch { }

            this.Guard = null;
        }

        /// <inheritdoc />
        public void StartGuard()
        {
            this.Guard.StartHeartBeat(this);
        }
    }
}
