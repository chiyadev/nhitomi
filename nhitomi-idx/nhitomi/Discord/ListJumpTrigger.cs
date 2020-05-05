using System;
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
            ListJumpTriggerDestination.Input => new Emoji("\ud83d\udcd1"),

            _ => null
        };

        public ListJumpTrigger(IListJumpTriggerTarget target, ListJumpTriggerDestination destination)
        {
            _target      = target;
            _destination = destination;
        }

        protected override async Task<ReactionTriggerResult> RunAsync(CancellationToken cancellationToken = default)
        {
            var l = Context.Locale.Sections["reactions.list"];

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

                case ListJumpTriggerDestination.Input:
                    var message = await Message.ListenAsync(l["jump"], cancellationToken);

                    if (int.TryParse(message.Content, out var page) && await _target.SetPositionAsync(Math.Clamp(page - 1, _target.Start, _target.End), cancellationToken))
                        return ReactionTriggerResult.Handled | ReactionTriggerResult.StateUpdated;

                    break;
            }

            return ReactionTriggerResult.Ignored;
        }
    }
}