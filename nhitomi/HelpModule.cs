// Copyright (c) 2018-2019 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using Microsoft.Extensions.Options;
using nhitomi.Core;

namespace nhitomi
{
    public class HelpModule : ModuleBase
    {
        readonly AppSettings _settings;
        readonly CommandService _commands;
        readonly ISet<IDoujinClient> _clients;

        public HelpModule(
            IOptions<AppSettings> options,
            CommandService commands,
            ISet<IDoujinClient> clients)
        {
            _settings = options.Value;
            _commands = commands;
            _clients = clients;
        }

        [Command("help")]
        [Summary("Shows this help message.")]
        public async Task HelpAsync()
        {
            // Reply with embedded help message
            await ReplyAsync(
                string.Empty,
                embed: MessageFormatter.EmbedHelp(
                    commands: _commands.Commands,
                    clients: _clients,
                    prefix: _settings.Discord.Prefix
                )
            );
        }
    }
}
