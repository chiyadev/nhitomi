// Copyright (c) 2018-2019 chiya.dev
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace nhitomi.Discord
{
    public class DiscordService : IDisposable
    {
        readonly AppSettings _settings;
        readonly ILogger<DiscordService> _logger;

        public BaseSocketClient Socket { get; }
        public CommandService Command { get; }

        public DiscordService(IOptions<AppSettings> options, ILogger<DiscordService> logger)
        {
            _settings = options.Value;
            _logger = logger;

            Socket = new DiscordSocketClient(_settings.Discord);
            Command = new CommandService(_settings.Discord.Command);
        }

        public async Task ConnectAsync()
        {
            if (Socket.LoginState != LoginState.LoggedOut)
                return;

            // login
            await Socket.LoginAsync(TokenType.Bot, _settings.Discord.Token);
            await Socket.StartAsync();
        }

        public void Dispose()
        {
            Socket.Dispose();
            // Commands.Dispose does not exist
        }
    }
}