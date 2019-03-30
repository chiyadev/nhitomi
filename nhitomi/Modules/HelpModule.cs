// Copyright (c) 2018-2019 fate/loli
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System.Threading.Tasks;
using Discord.Commands;

namespace nhitomi.Modules
{
    public class HelpModule : ModuleBase
    {
        readonly MessageFormatter _formatter;

        public HelpModule(
            MessageFormatter formatter)
        {
            _formatter = formatter;
        }

        [Command("help")]
        [Summary("Shows this help message.")]
        public Task HelpAsync() => ReplyAsync(embed: _formatter.CreateHelpEmbed());
    }
}