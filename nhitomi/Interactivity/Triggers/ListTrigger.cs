using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;

namespace nhitomi.Interactivity.Triggers
{
    public enum MoveDirection
    {
        Left,
        Right
    }

    public class ListTrigger<T> : ReactionTrigger<ListInteractiveMessage<T>>
    {
        readonly MoveDirection _direction;

        public override string Name => $"Move {_direction}";

        public override IEmote Emote
        {
            get
            {
                switch (_direction)
                {
                    // left arrow
                    case MoveDirection.Left: return new Emoji("\u25c0");

                    // right arrow
                    case MoveDirection.Right: return new Emoji("\u25b6");
                }

                throw new ArgumentException(nameof(_direction));
            }
        }

        public ListTrigger(MoveDirection direction)
        {
            _direction = direction;
        }

        public override async Task RunAsync(IServiceProvider services, CancellationToken cancellationToken = default)
        {
            switch (_direction)
            {
                case MoveDirection.Left:
                    await Interactive.PreviousAsync(services, cancellationToken);
                    break;

                case MoveDirection.Right:
                    await Interactive.NextAsync(services, cancellationToken);
                    break;
            }
        }
    }
}