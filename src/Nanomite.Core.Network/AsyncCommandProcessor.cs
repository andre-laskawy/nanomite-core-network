///-----------------------------------------------------------------
///   File:         AsyncCommandProcessor.cs
///   Author:   	Andre Laskawy           
///   Date:         03.10.2018 11:52:53
///-----------------------------------------------------------------

namespace Nanomite.Core.Network
{
    using Google.Protobuf.WellKnownTypes;
    using Nanomite.Core.Network.Common;
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="AsyncCommandProcessor"/>
    /// </summary>
    public class AsyncCommandProcessor
    {
        /// <summary>
        /// The awaiting group identifier list
        /// </summary>
        private static ConcurrentDictionary<string, string> awaitingGroupIdList = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Defines the receipts that has been received
        /// </summary>
        private static ConcurrentDictionary<string, Command> receipts = new ConcurrentDictionary<string, Command>();

        /// <inheritdoc />
        public static async Task<bool> Notify(Command cmd)
        {
            if (awaitingGroupIdList.Count > 0
                && !string.IsNullOrEmpty(cmd.TargetId))
            {
                string key = cmd.Topic + "/" + cmd.TargetId;
                if (awaitingGroupIdList.ContainsKey(key)) // used for smoke tests
                {
                    // remove imediatly
                    string msgId = null;
                    while (!awaitingGroupIdList.TryRemove(key, out msgId))
                    {
                        await Task.Delay(1);
                    }

                    // add data object
                    while (!receipts.TryAdd(key, cmd))
                    {
                        await Task.Delay(1);
                    }

                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public static async Task<Command> ProcessCommand(Command cmd, int timeout = 60)
        {
            try
            {
                if (string.IsNullOrEmpty(cmd.SenderId))
                {
                    throw new Exception("Only commands with a defined sender can be processed");
                }

                // Wait for result
                string key = cmd.Topic + "/" + cmd.SenderId;
                return await WaitForReceipt(key, timeout);
            }
            catch (Exception ex)
            {
                throw new Exception("Unknown process error occured. See inner exception for details", ex);
            }
        }

        /// <summary>
        /// Waits for receipt.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        private static async Task<Command> WaitForReceipt(string key, int timeout)
        {
            try
            {
                // add group id
                while (!awaitingGroupIdList.TryAdd(key, key))
                {
                    await Task.Delay(1);
                }

                // wait until message with id is received or timeout is thrown
                await new Func<bool>(() => { return receipts.Any(p => p.Key == key); }).AwaitResult(timeout);

                // get receipt and remove from awaiting receipts list
                Command receipt = null;
                while (!receipts.TryRemove(key, out receipt))
                {
                    await Task.Delay(1);
                }

                return receipt;
            }
            catch (Exception ex)
            {
                string msg = "Error while awaiting command receipt for command with key: " + key;
                var cmd = new Command() { Topic = key };
                cmd.Data.Add(Any.Pack(new Exception(msg, ex).ToProtoException()));
                return cmd;
            }
        }
    }
}
