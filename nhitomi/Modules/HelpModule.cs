// Copyright (c) 2018-2019 chiya.dev
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System.Threading.Tasks;
using nhitomi.Discord;
using nhitomi.Discord.Parsing;
using nhitomi.Interactivity;

namespace nhitomi.Modules
{
    [Module("help", IsPrefixed = false)]
    public class HelpModule
    {
        readonly IDiscordContext _context;
        readonly InteractiveManager _interactive;

        public HelpModule(IDiscordContext context, InteractiveManager interactive)
        {
            _context = context;
            _interactive = interactive;
        }

        [Command("help")]
        public Task HelpAsync() => _interactive.SendInteractiveAsync(new HelpMessage(), _context);
    }
}