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

        protected override async Task<bool> RunAsync(CancellationToken cancellationToken = default)
        {
            switch (_destination)
            {
                case ListJumpTriggerDestination.Start:
                    return await _target.SetPosition(_target.Start, cancellationToken);

                case ListJumpTriggerDestination.End:
                    return await _target.SetPosition(_target.End, cancellationToken);

                default:
                    return false;
            }
        }
    }
}