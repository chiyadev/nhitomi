using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;

namespace nhitomi.Discord
{
    [Flags]
    public enum ReactionTriggerResult
    {
        /// <summary>
        /// Reaction was ignored and no action was taken.
        /// </summary>
        Ignored = 0,

        /// <summary>
        /// Reaction was handled but interactive state is untouched.
        /// </summary>
        Handled = 1,

        /// <summary>
        /// Reaction was handled and interactive state was modified.
        /// </summary>
        StateUpdated = 1 << 1
    }

    /// <summary>
    /// Represents a trigger that operates on an interactive message and alters its state, using Discord reactions.
    /// </summary>
    public abstract class ReactionTrigger : IDisposable
    {
        /// <summary>
        /// Emote of the reaction.
        /// </summary>
        public abstract IEmote Emote { get; }

        InteractiveMessage _message;

        /// <summary>
        /// Interactive that this trigger is attached to.
        /// </summary>
        public InteractiveMessage Message => _message;

        /// <inheritdoc cref="InteractiveMessage.Context"/>
        public nhitomiCommandContext Context => Message.Context;

        /// <inheritdoc cref="InteractiveMessage.Services"/>
        public IServiceProvider Services => Message.Services;

        internal void Initialize(InteractiveMessage message)
        {
            if (Interlocked.CompareExchange(ref _message, message, null) != null)
                throw new InvalidOperationException($"Reaction trigger {GetType()} is already initialized.");
        }

        /// <summary>
        /// Invokes this trigger. Returns true if trigger was run successfully and the interactive was rerendered.
        /// </summary>
        public async Task<bool> InvokeAsync(CancellationToken cancellationToken = default)
        {
            var message = _message;

            if (message == null)
                return false;

            ReactionTriggerResult result;

            using (await _message.Semaphore.EnterAsync(cancellationToken))
                result = await RunAsync(cancellationToken);

            if (result.HasFlag(ReactionTriggerResult.StateUpdated))
                await message.RerenderAsync(cancellationToken);

            return result.HasFlag(ReactionTriggerResult.Handled);
        }

        /// <summary>
        /// Alters the state of an interactive message.
        /// Interactive is automatically locked during this method, and rerendered if this method returns true.
        /// </summary>
        protected abstract Task<ReactionTriggerResult> RunAsync(CancellationToken cancellationToken = default);

        public virtual void Dispose() => _message = null;
    }
}