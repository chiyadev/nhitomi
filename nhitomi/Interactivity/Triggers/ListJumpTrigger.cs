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
        readonly int _endPosition;

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

        public ListJumpTrigger(JumpDestination destination, int endPosition = 0)
        {
            _destination = destination;
            _endPosition = endPosition;
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

                switch (Trigger._destination)
                {
                    case JumpDestination.Start:
                        if (Interactive.Position == 0)
                            Interactive.Position = -1;
                        else
                            Interactive.Position = 0;
                        break;

                    case JumpDestination.End:
                        if (Interactive.Position == Trigger._endPosition)
                            Interactive.Position = Trigger._endPosition + 1;
                        else
                            Interactive.Position = Trigger._endPosition;
                        break;
                }

                return await Interactive.UpdateViewAsync(_services, cancellationToken);
            }
        }
    }
}