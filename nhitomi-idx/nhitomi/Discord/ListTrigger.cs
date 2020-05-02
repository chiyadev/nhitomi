using System.Threading;
using System.Threading.Tasks;
using Discord;

namespace nhitomi.Discord
{
    public enum ListTriggerDirection
    {
        Left,
        Right
    }

    public interface IListTriggerTarget
    {
        int Position { get; }

        Task<bool> SetPositionAsync(int position, CancellationToken cancellationToken = default);
    }

    public class ListTrigger : ReactionTrigger
    {
        readonly IListTriggerTarget _target;
        readonly ListTriggerDirection _direction;

        public override IEmote Emote => _direction switch
        {
            ListTriggerDirection.Left  => new Emoji("\u25c0"),
            ListTriggerDirection.Right => new Emoji("\u25b6"),

            _ => null
        };

        public ListTrigger(IListTriggerTarget target, ListTriggerDirection direction)
        {
            _target    = target;
            _direction = direction;
        }

        protected override async Task<ReactionTriggerResult> RunAsync(CancellationToken cancellationToken = default)
        {
            switch (_direction)
            {
                case ListTriggerDirection.Left:
                    if (_target.Position > 0 && await _target.SetPositionAsync(_target.Position - 1, cancellationToken))
                        return ReactionTriggerResult.Handled | ReactionTriggerResult.StateUpdated;

                    break;

                case ListTriggerDirection.Right:
                    if (await _target.SetPositionAsync(_target.Position + 1, cancellationToken))
                        return ReactionTriggerResult.Handled | ReactionTriggerResult.StateUpdated;

                    break;
            }

            return ReactionTriggerResult.Ignored;
        }
    }
}