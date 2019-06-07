using System.Threading;
using System.Threading.Tasks;
using nhitomi.Discord;
using nhitomi.Discord.Parsing;
using nhitomi.Interactivity;

namespace nhitomi.Modules
{
    [Module("help", IsPrefixed = false)]
    public class HelpModule
    {
        readonly IMessageContext _context;
        readonly InteractiveManager _interactive;

        public HelpModule(IMessageContext context, InteractiveManager interactive)
        {
            _context = context;
            _interactive = interactive;
        }

        [Command("help")]
        public Task HelpAsync(CancellationToken cancellationToken = default) =>
            _interactive.SendInteractiveAsync(new HelpMessage(), _context, cancellationToken);
    }
}