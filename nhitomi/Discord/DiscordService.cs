// Copyright (c) 2018-2019 chiya.dev
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace nhitomi.Discord
{
    public class DiscordService : IDisposable
    {
        readonly AppSettings _settings;

        public BaseSocketClient Socket { get; }
        public CommandService Command { get; }

        public DiscordService(IOptions<AppSettings> options)
        {
            _settings = options.Value;

            Socket = new DiscordSocketClient(_settings.Discord);
            Command = new CommandService(_settings.Discord.Command);
        }

        public async Task ConnectAsync()
        {
            if (Socket.LoginState != LoginState.LoggedOut)
                return;

            // login
            switch (Socket)
            {
                case BaseDiscordClient client:
                    await client.LoginAsync(TokenType.Bot, _settings.Discord.Token);
                    break;
            }

            // start
            await Socket.StartAsync();
        }

        public async Task DisconnectAsync()
        {
            if (Socket.LoginState != LoginState.LoggedIn)
                return;

            // stop
            await Socket.StopAsync();

            // logout
            switch (Socket)
            {
                case BaseDiscordClient client:
                    await client.LogoutAsync();
                    break;
            }
        }

        public void Dispose() => Socket.Dispose();
        // Commands.Dispose does not exist
    }
}