///-----------------------------------------------------------------
///   File:         GrpcGuard.cs
///   Author:   	Andre Laskawy           
///   Date:         30.09.2018 16:24:14
///-----------------------------------------------------------------

namespace Nanomite.Core.Network.Grpc.Models
{
    using Nanomite.Core.Network.Common;
    using Nanomite.Core.Network.Common.Models;
    using System;
    using System.Threading.Tasks;
    using System.Timers;

    /// <summary>
    /// The ConnectionLost
    /// </summary>
    /// <param name="streamId">The <see cref="string"/></param>
    public delegate void ConnectionLost(string streamId);

    /// <summary>
    /// Defines the <see cref="SocketGuard" />
    /// </summary>
    public class GrpcGuard
    {
        /// <summary>
        /// Defines the timer which sends the heartbeat to the server
        /// </summary>
        private Timer heardBeatTimer = null;

        /// <summary>
        /// The stream
        /// </summary>
        private IStream<Command> stream = null;

        /// <summary>
        /// Gets or sets the action will be executed if the heartbeat is not successful
        /// </summary>
        public Func<IStream<Command>, Task> OnConnectionLost { get; set; }

        /// <summary>
        /// Gets or sets the eastablish connection command.
        /// </summary>
        public Action EstablishConnection { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GrpcGuard"/> class.
        /// </summary>
        /// <param name="heartbeat">The action which is used to send the heartbeat<see cref="Action{Command}"/></param>
        public GrpcGuard(Action<Command, int> heartbeat)
        {
            this.OnHeartbeat = heartbeat;
        }

        /// <summary>
        /// Gets or sets the action is used to send the heartbeat
        /// </summary>
        public Action<Command, int> OnHeartbeat { get; set; }

        /// <summary>
        /// Starts the timer to send the heartbeat in interval
        /// </summary>
        /// <param name="stream">The stream<see cref="IStream{Command}"/></param>
        public void StartHeartBeat(IStream<Command> stream)
        {
            if (this.OnHeartbeat != null)
            {
                this.stream = stream;
                this.heardBeatTimer = new Timer();
                if (this.stream.Connected)
                {
                    this.heardBeatTimer.Interval = 5000;
                }
                else
                {
                    this.heardBeatTimer.Interval = 500;
                }

                this.heardBeatTimer.Elapsed += CheckConnection;
                this.heardBeatTimer.AutoReset = false;
                this.heardBeatTimer.Start();
            }
        }

        /// <summary>
        /// Checks if connection is established
        /// </summary>
        /// <returns>The <see cref="bool"/></returns>
        public bool IsConnected()
        {
            bool result = true;

            try
            {
                if (stream != null)
                {
                    return false;
                }

                if (this.OnHeartbeat != null)
                {
                    Command message = new Command() { Topic = StaticCommandKeys.HeartBeat };
                    this.OnHeartbeat?.Invoke(message, 3);
                }
            }
            catch
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        /// The timer method that will be executed by the heartbeat timer
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="System.Timers.ElapsedEventArgs"/></param>
        private async void CheckConnection(object sender, System.Timers.ElapsedEventArgs e)
        {
            bool connectionExists = this.IsConnected();
            if (!connectionExists)
            {
                this.stream.Connected = false;
                await OnConnectionLost?.Invoke(this.stream);
            }
            else if (connectionExists)
            {
                if (!this.stream.Connected)
                {
                    this.EstablishConnection?.Invoke();
                }
                this.heardBeatTimer.Interval = 5000;
                this.heardBeatTimer.Start();
            }
        }
    }
}
