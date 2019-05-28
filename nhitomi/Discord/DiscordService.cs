// Copyright (c) 2018-2019 chiya.dev
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace nhitomi.Discord
{
    public interface IDiscordContext
    {
        IUserMessage Message { get; }
        IMessageChannel Channel { get; }

        IUser User { get; }
    }

    public class DiscordService : DiscordSocketClient
    {
        readonly AppSettings _settings;

        public DiscordService(IOptions<AppSettings> options) : base(options.Value.Discord)
        {
            _settings = options.Value;
        }

        public async Task ConnectAsync()
        {
            if (LoginState != LoginState.LoggedOut)
                return;

            // login
            await LoginAsync(TokenType.Bot, _settings.Discord.Token);
            await StartAsync();
        }
    }
}