// Copyright (c) 2018-2019 chiya.dev
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System.Threading.Tasks;
using Discord.Commands;
using nhitomi.Interactivity;

namespace nhitomi.Modules
{
    public class HelpModule : ModuleBase
    {
        readonly InteractiveManager _interactive;

        public HelpModule(InteractiveManager interactive)
        {
            _interactive = interactive;
        }

        [Command("help")]
        public Task HelpAsync() => _interactive.SendInteractiveAsync(new HelpMessage(), Context);
    }
}