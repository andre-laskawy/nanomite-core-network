using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nanomite.Core.Network.Grpc
{
    public static class CommandExtensions
    {
        /// <summary>
        /// Converts a response to a exception
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="throwException">The throwException<see cref="bool" /></param>
        /// <returns>
        /// the proto Exception
        /// </returns>
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
