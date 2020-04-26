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

        protected override async Task<bool> RunAsync(CancellationToken cancellationToken = default)
        {
            switch (_direction)
            {
                case ListTriggerDirection.Left:
                    if (_target.Position == 0)
                        return false;

                    return await _target.SetPositionAsync(_target.Position - 1, cancellationToken);

                case ListTriggerDirection.Right:
                    return await _target.SetPositionAsync(_target.Position + 1, cancellationToken);

                default:
                    return false;
            }
        }
    }
}