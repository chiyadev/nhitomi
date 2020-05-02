using System.Threading;
using System.Threading.Tasks;
using Discord;

namespace nhitomi.Discord
{
    public enum ListJumpTriggerDestination
    {
        Start,
        End,
        Input
    }

    public interface IListJumpTriggerTarget : IListTriggerTarget
    {
        int Start => 0;
        int End { get; }
    }

    public class ListJumpTrigger : ReactionTrigger
    {
        readonly IListJumpTriggerTarget _target;
        readonly ListJumpTriggerDestination _destination;

        public override IEmote Emote => _destination switch
        {
            ListJumpTriggerDestination.Start => new Emoji("\u23EA"),
            ListJumpTriggerDestination.End   => new Emoji("\u23E9"),

            _ => null
        };

        public ListJumpTrigger(IListJumpTriggerTarget target, ListJumpTriggerDestination destination)
        {
            _target      = target;
            _destination = destination;
        }

        protected override async Task<ReactionTriggerResult> RunAsync(CancellationToken cancellationToken = default)
        {
            switch (_destination)
            {
                case ListJumpTriggerDestination.Start:
                    if (await _target.SetPositionAsync(_target.Start, cancellationToken))
                        return ReactionTriggerResult.Handled | ReactionTriggerResult.StateUpdated;

                    break;

                case ListJumpTriggerDestination.End:
                    if (await _target.SetPositionAsync(_target.End, cancellationToken))
                        return ReactionTriggerResult.Handled | ReactionTriggerResult.StateUpdated;

                    break;
            }

            return ReactionTriggerResult.Ignored;
        }
    }
}