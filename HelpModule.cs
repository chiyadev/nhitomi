using System.Collections.Generic;
using Discord.Commands;

namespace nhitomi
{
    public class HelpModule : ModuleBase
    {
        readonly MessageFormatter _formatter;
        readonly InteractiveScheduler _interactive;
        readonly ISet<IDoujinClient> _clients;

        public HelpModule(
            MessageFormatter formatter,
            InteractiveScheduler interactive,
            ISet<IDoujinClient> clients
        )
        {
            _formatter = formatter;
            _interactive = interactive;
            _clients = clients;
        }
    }
}