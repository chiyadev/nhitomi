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
        [Summary("Shows the help message.")]
        public async Task HelpAsync()
        {
            // Let the user know
            await ReplyAsync($"**nhitomi**: Help sent in DM!");

            // Create DM channel for the requester
            var channel = await Context.User.GetOrCreateDMChannelAsync();

            // Send embedded help message
            await channel.SendMessageAsync(
                text: null,
                embed: _formatter.EmbedHelp(
                    commands: _commands.Commands,
                    clients: _clients
                )
            );
        }
    }
}