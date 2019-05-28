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

    public class ListTrigger : ReactionTrigger<ListTrigger.Action>
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

        protected override void InitializeAction(Action action)
        {
            base.InitializeAction(action);

            action.Direction = _direction;
        }

        public class Action : ActionBase<IListMessage>
        {
            public MoveDirection Direction;

            readonly IServiceProvider _services;

            public Action(IServiceProvider services)
            {
                _services = services;
            }

            public override async Task<bool> RunAsync(CancellationToken cancellationToken = default)
            {
                if (!await base.RunAsync(cancellationToken))
                    return false;

                switch (Direction)
                {
                    case MoveDirection.Left:
                        Interactive.Position += 1;
                        break;

                    case MoveDirection.Right:
                        Interactive.Position += 1;
                        break;
                }

                return await Interactive.UpdateViewAsync(_services, Context, cancellationToken);
            }
        }
    }
}