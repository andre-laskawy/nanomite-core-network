///-----------------------------------------------------------------
///   File:         TokenObserver.cs
///   Author:   	Andre Laskawy           
///   Date:         03.10.2018 15:36:55
///-----------------------------------------------------------------

namespace Nanomite.Core.Network
{
    using Google.Protobuf.WellKnownTypes;
    using Nanomite.Core.Network.Common;
    using Nanomite.Core.Network.Common.Models;
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="TokenObserver" />
    /// </summary>
    public class TokenObserver
    {
        /// <summary>
        /// The token collection
        /// </summary>
        private static ConcurrentDictionary<string, NetworkUser> tokenCollection = new ConcurrentDictionary<string, NetworkUser>();

        /// <summary>
        /// The timer which is refreshing the tokens
        /// </summary>
        private static Timer observingTimer;

        /// <summary>
        /// Defines wether the timer is busy
        /// </summary>
        private static bool IsBusy = false;

        /// <summary>
        /// The client
        /// </summary>
        private NanomiteClient client = null;

        /// <summary>
        /// The is connected
        /// </summary>
        private bool isConnected = false;

        /// <summary>
        /// Initializes static members of the <see cref="TokenObserver" /> class.
        /// </summary>
        /// <param name="authServerAddress">The authentication server address.</param>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="user">The user.</param>
        /// <param name="interval">The interval.</param>
        public TokenObserver(string authServerAddress, string clientId, int interval = 1000 * 60)
        {
            this.client = NanomiteClient.CreateGrpcClient(authServerAddress, clientId);
            observingTimer = new Timer(Run, null, 0, interval);
        }

        /// <summary>
        /// Initializes the observer and tries to connect to the auth server.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="secret">The secret.</param>
        /// <returns></returns>
        public async Task Init(string loginName, string password, string secret)
        {
            await this.client.ConnectAsync(loginName, password, secret, true);
            this.client.OnDisconnected = (id) =>
            {
                this.isConnected = false;
            };
            this.client.OnConnected = () =>
            {
                this.isConnected = true;
            };
        }

        /// <summary>
        /// Checks if the received token is valid
        /// </summary>
        /// <param name="token">The token<see cref="string"/></param>
        /// <returns>The <see cref="Task{bool}"/></returns>
        public async Task<NetworkUser> IsValid(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            if (tokenCollection.ContainsKey(token))
            {
                return tokenCollection[token];
            }
            else
            {
                NetworkUser user = new NetworkUser();
                user.AuthenticationToken = token;
                return await TokenIsValid(user);
            }
        }

        /// <summary>
        /// Runs the timer which will check if the tokens are still valid
        /// </summary>
        /// <param name="state">The state<see cref="object"/></param>
        public async void Run(object state)
        {
            if (IsBusy || !isConnected)
            {
                return;
            }

            try
            {
                IsBusy = true;

                var tokens = tokenCollection.Keys.ToList();
                foreach (var token in tokens)
                {
                    NetworkUser user = tokenCollection[token];
                    user.AuthenticationToken = token;
                    if (await TokenIsValid(user) == null)
                    {
                        NetworkUser u = null;
                        while (!tokenCollection.TryRemove(token, out u))
                        {
                            await Task.Delay(1);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Tokens the is valid.
        /// </summary>
        /// <param name="user">The user<see cref="NetworkUser" /></param>
        /// <returns>
        /// The <see cref="Task{bool}" /></returns>
        private async Task<NetworkUser> TokenIsValid(NetworkUser user)
        {
            Command cmd = new Command() { Type = CommandType.Action, Topic = StaticCommandKeys.TokenValidation };
            cmd.Data.Add(Any.Pack(user));

            var result = await this.client.SendCommandAsync(cmd);
            if (result != null && result.Data.Count > 0)
            {
                var newUser = result.Data.FirstOrDefault().CastToModel<NetworkUser>();
                if (!string.IsNullOrEmpty(newUser.AuthenticationToken))
                {
                    NetworkUser old = null;
                    if (newUser.AuthenticationToken != user.AuthenticationToken)
                    {
                        while (!tokenCollection.TryRemove(user.AuthenticationToken, out old))
                        {
                            await Task.Delay(1);
                        }
                    }

                    if (!tokenCollection.ContainsKey(user.AuthenticationToken))
                    {
                        while (!tokenCollection.TryAdd(newUser.AuthenticationToken, newUser))
                        {
                            await Task.Delay(1);
                        }
                    }

                    return newUser;
                }
            }

            return null;
        }
    }
}
