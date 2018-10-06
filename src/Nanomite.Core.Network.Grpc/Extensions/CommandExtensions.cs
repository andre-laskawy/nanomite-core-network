///-----------------------------------------------------------------
///   File:         CommandExtensions.cs
///   Author:   	Andre Laskawy           
///   Date:         03.10.2018 15:05:21
///-----------------------------------------------------------------

namespace Nanomite.Core.Network.Grpc
{
    using Nanomite.Core.Network.Common;
    using System;
    using System.Linq;

    /// <summary>
    /// Defines the <see cref="CommandExtensions" />
    /// </summary>
    public static class CommandExtensions
    {
        /// <summary>
        /// Converts a response to a exception
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="throwException">The throwException<see cref="bool" /></param>
        /// <returns>The <see cref="Exception"/></returns>
        public static Exception ToException(this GrpcResponse response, bool throwException = false)
        {
            if (response.Result != ResultCode.Error)
            {
                return null;
            }

            if (response.Data.Count > 0)
            {
                return response.Data.FirstOrDefault().ToException(throwException);
            }
            else
            {
                Exception exception = new Exception(response.Message);
                if (throwException)
                {
                    throw exception;
                }
                else
                {
                    return exception;
                }

            }
        }
    }
}
