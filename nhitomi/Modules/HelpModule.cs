// Copyright (c) 2018-2019 chiya.dev
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System.Threading.Tasks;
using nhitomi.Discord.Parsing;
using nhitomi.Interactivity;

namespace nhitomi.Modules
{
    [Module("help", IsPrefixed = false)]
    public class HelpModule
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