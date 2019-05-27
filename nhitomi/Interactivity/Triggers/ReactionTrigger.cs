using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace nhitomi.Interactivity.Triggers
{
    public abstract class ReactionTrigger
    {
        public abstract string Name { get; }
        public abstract IEmote Emote { get; }

        /// <summary>
        /// Whether this action can be triggered without a fully initialized interactive system.
        /// </summary>
        public virtual bool CanRunStateless => false;

        protected InteractiveMessage Interactive { get; private set; }
        protected ICommandContext Context { get; private set; }
        protected IUserMessage Message { get; private set; }

        public virtual void Initialize(InteractiveMessage interactive)
        {
            Interactive = interactive;

            Initialize(interactive.Context, interactive.Message);
        }

        public void Initialize(ICommandContext context, IUserMessage message)
        {
            Context = context;
            Message = message;
        }

        public abstract Task RunAsync(IServiceProvider services, CancellationToken cancellationToken = default);
    }

    public abstract class ReactionTrigger<TMessage> : ReactionTrigger
        where TMessage : class
    {
        protected new TMessage Interactive => base.Interactive as TMessage;

        public override void Initialize(InteractiveMessage interactive)
        {
            if (!(interactive is TMessage))
                throw new InvalidOperationException(
                    $"Cannot attach {GetType().Name} to a {interactive.GetType().Name} interactive.");

            base.Initialize(interactive);
        }
    }
}