using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;

namespace nhitomi.Interactivity.Triggers
{
    public enum JumpDestination
    {
        Start,
        End
    }

    public class ListJumpTrigger : ReactionTrigger<ListJumpTrigger.Action>
    {
        readonly JumpDestination _destination;
        readonly int _position;

        public override string Name => $"Jump to {_destination}";

        public override IEmote Emote
        {
            get
            {
                switch (_destination)
                {
                    // left arrow
                    case JumpDestination.Start: return new Emoji("\u23EA");

                    // right arrow
                    case JumpDestination.End: return new Emoji("\u23E9");
                }

                throw new ArgumentException(nameof(_destination));
            }
        }

        public ListJumpTrigger(JumpDestination destination, int position)
        {
            _destination = destination;
            _position = position;
        }

        public class Action : ActionBase<IListMessage>
        {
            new ListJumpTrigger Trigger => (ListJumpTrigger) base.Trigger;

            readonly IServiceProvider _services;

            public Action(IServiceProvider services)
            {
                _services = services;
            }

            public override async Task<bool> RunAsync(CancellationToken cancellationToken = default)
            {
                if (!await base.RunAsync(cancellationToken))
                    return false;

                Interactive.Position = Trigger._position;

                return await Interactive.UpdateViewAsync(_services, cancellationToken);
            }
        }
    }
}