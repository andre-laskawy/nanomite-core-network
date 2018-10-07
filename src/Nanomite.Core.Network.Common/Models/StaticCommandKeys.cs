///-----------------------------------------------------------------
///   File:         StaticCommandKeys.cs
///   Author:   	Andre Laskawy           
///   Date:         02.10.2018 18:04:34
///-----------------------------------------------------------------

namespace Nanomite.Core.Network.Common.Models
{
    /// <summary>
    /// Defines the <see cref="StaticCommandKeys" />
    /// </summary>
    public class StaticCommandKeys
    {
        /// <summary>
        /// Defines the OpenStream
        /// </summary>
        public const string OpenStream = "OpenStream";

        /// <summary>
        /// Defines the HeartBeat
        /// </summary>
        public const string HeartBeat = "HeartBeat";

        /// <summary>
        /// Defines the Connect
        /// </summary>
        public const string Connect = "Connect";

        /// <summary>
        /// The token validation
        /// </summary>
        public const string TokenValidation = "TokenValidation";

        /// <summary>
        /// The subscribe
        /// </summary>
        public const string Subscribe = "Subscribe";

        /// <summary>
        /// The unsubscribe
        /// </summary>
        public const string Unsubscribe = "Unsubscribe";

        /// <summary>
        /// The file transfer
        /// </summary>
        public const string FileTransfer = "FileTransfer";

        /// <summary>
        /// The service meta data
        /// </summary>
        public const string ServiceMetaData = "ServiceMetaData";
    }
}
