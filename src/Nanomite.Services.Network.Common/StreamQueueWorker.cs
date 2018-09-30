///-----------------------------------------------------------------
///   File:         StreamQueueWorker.cs
///   Author:   	Andre Laskawy           
///   Date:         30.09.2018 16:10:20
///-----------------------------------------------------------------

namespace Nanomite.Services.Network.Common
{
    using Google.Protobuf;
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="StreamQueueWorker{T}"/>
    /// </summary>
    /// <typeparam name="T">any type of IPackageData</typeparam>
    public class StreamQueueWorker<T> where T : IMessage
    {
        /// <summary>
        /// Gets or sets the action that is called when a send exception occures
        /// </summary>
        public static Action<Exception> OnException { get; set; }

        /// <summary>
        /// Defines the package queue wich consists of all packages that need to be send
        /// </summary>
        private ConcurrentQueue<T> packages = new ConcurrentQueue<T>();

        /// <summary>
        /// Defines the stream, which can be defined by any technlogy The stream is responsible for
        /// actual sending of the package
        /// </summary>
        private IStream<T> stream;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamQueueWorker{T}"/> class.
        /// </summary>
        /// <param name="stream">The <see cref="IStream"/></param>
        public StreamQueueWorker(IStream<T> stream)
        {
            this.stream = stream;
            this.Run();
        }

        /// <summary>
        /// Runs long running task which is iterating over a queue wich contains all commands that need to be send on the specific stream.
        /// Used to prevent concurrency problems on the channel.
        /// </summary>
        public void Run()
        {
            new Task(async () =>
            {
                while (true)
                {
                    await Task.Delay(10);
                    try
                    {
                        while (this.packages.Count > 0)
                        {
                            T currentItem;
                            if (this.packages.TryPeek(out currentItem))
                            {
                                if (this.stream.Locked)
                                {
                                    continue;
                                }
                                else
                                {
                                    if (this.stream.Connected)
                                    {
                                        while (!this.packages.TryDequeue(out currentItem))
                                        {
                                            await Task.Delay(10);
                                        }
                                        await this.stream.WriteAsync(currentItem);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        OnException?.Invoke(ex);
                    }
                }
            }, TaskCreationOptions.LongRunning).Start();
        }

        /// <summary>
        /// Adds a package to the queue. The package will be send as soon the packag is reached
        /// inside the queue
        /// </summary>
        /// <param name="message">The <see cref="T"/></param>
        public void Send(T message)
        {
            this.packages.Enqueue(message);
        }
    }
}
