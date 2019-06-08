using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using nhitomi.Discord;

namespace nhitomi.Interactivity.Triggers
{
    public interface IReactionTrigger
    {
        string Name { get; }
        IEmote Emote { get; }

        /// <summary>
        /// Whether this action can be triggered without a fully initialized interactive system.
        /// </summary>
        bool CanRunStateless { get; }

        Task<bool> RunAsync(IServiceProvider services, IInteractiveMessage interactive,
            CancellationToken cancellationToken = default);
    }

    public abstract class ReactionTrigger<TAction> : IReactionTrigger
        where TAction : ReactionTrigger<TAction>.ActionBase
    {
        public abstract string Name { get; }
        public abstract IEmote Emote { get; }

        public virtual bool CanRunStateless => false;

        static readonly DependencyFactory<TAction> _actionFactory = DependencyUtility<TAction>.Factory;

        public Task<bool> RunAsync(IServiceProvider services, IInteractiveMessage interactive,
            CancellationToken cancellationToken = default)
        {
            if (interactive == null && !CanRunStateless)
                throw new InvalidOperationException($"Cannot initialize trigger {GetType()} in stateless mode.");

            // create action object
            var action = _actionFactory(services);
            action.Trigger = this;
            action.Interactive = interactive;
            action.Context = services.GetRequiredService<IDiscordContext>();

            // trigger the action
            return action.RunAsync(cancellationToken);
        }

        public abstract class ActionBase
        {
            public ReactionTrigger<TAction> Trigger { get; set; }
            public IInteractiveMessage Interactive { get; set; }
            public IDiscordContext Context { get; set; }

            public abstract Task<bool> RunAsync(CancellationToken cancellationToken = default);
        }

        public abstract class ActionBase<TInteractive> : ActionBase
            where TInteractive : IInteractiveMessage
        {
            public new TInteractive Interactive => (TInteractive) base.Interactive;

            public override Task<bool> RunAsync(CancellationToken cancellationToken = default) =>
                Task.FromResult(base.Interactive == null || base.Interactive is TInteractive);
        }
    }
}