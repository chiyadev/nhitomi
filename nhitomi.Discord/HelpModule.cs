using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;

namespace nhitomi
{
    public class HelpModule : ModuleBase
    {
        readonly CommandService _commands;
        readonly MessageFormatter _formatter;
        readonly ISet<IDoujinClient> _clients;

        public HelpModule(
            CommandService commands,
            MessageFormatter formatter,
            ISet<IDoujinClient> clients
        )
        {
            _commands = commands;
            _formatter = formatter;
            _clients = clients;
        }

        [Command("help")]
        [Summary("Shows this help message.")]
        public async Task HelpAsync()
        {
            // Reply with embedded help message
            await ReplyAsync(
                message: string.Empty,
                embed: _formatter.EmbedHelp(
                    commands: _commands.Commands,
                    clients: _clients
                )
            );
        }
    }
}