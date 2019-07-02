using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using nhitomi.Discord;
using nhitomi.Interactivity.Triggers;

namespace nhitomi.Interactivity
{
    public interface IInteractiveMessage : IEmbedMessage, IDisposable
    {
        IReadOnlyDictionary<IEmote, IReactionTrigger> Triggers { get; }
    }

    public abstract class InteractiveMessage<TView> : EmbedMessage<TView>, IInteractiveMessage
        where TView : EmbedMessage<TView>.ViewBase
    {
        public IReadOnlyDictionary<IEmote, IReactionTrigger> Triggers { get; private set; }

        protected abstract IEnumerable<IReactionTrigger> CreateTriggers();

        readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public override async Task<bool> UpdateViewAsync(IServiceProvider services,
                                                         CancellationToken cancellationToken = default)
        {
            try
            {
                // disallow concurrent view updates
                await _semaphore.WaitAsync(cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                // message was deleted
                return false;
            }

            try
            {
                if (!await base.UpdateViewAsync(services, cancellationToken))
                    return false;

                if (Triggers == null)
                {
                    // initialize reaction triggers
                    Triggers = CreateTriggers().ToDictionary(t => t.Emote);

                    // enqueue adding reactions
                    // this is to avoid blocking the command handling thread with reaction rate limiting
                    services.GetService<InteractiveManager>()?.EnqueueReactions(Message, Triggers.Keys);
                }

                return true;
            }
            finally
            {
                try
                {
                    _semaphore.Release();
                }
                catch (ObjectDisposedException)
                {
                    // message was deleted
                }
            }
        }

        readonly Queue<InteractiveViewState> _pendingStates = new Queue<InteractiveViewState>();

        struct InteractiveViewState
        {
            public IDiscordContext Context;
            public Optional<string> Message;
            public Optional<Embed> Embed;

            public InteractiveViewState(IDiscordContext context,
                                        Optional<string> message,
                                        Optional<Embed> embed)
            {
                Context = context;
                Message = message;
                Embed   = embed;
            }
        }

        DateTime _lastUpdateTime;

        protected override async Task UpdateMessageAsync(IDiscordContext context,
                                                         Optional<string> message,
                                                         Optional<Embed> embed,
                                                         CancellationToken cancellationToken = default)
        {
            var currentTime = DateTime.Now;
            var timeToWait  = _lastUpdateTime + TimeSpan.FromSeconds(1) - currentTime;

            // if updating quickly (more than once per second)
            if (timeToWait.Ticks > 0)
                lock (_pendingStates)
                {
                    // queue current state to update in the background
                    _pendingStates.Enqueue(new InteractiveViewState(context, message, embed));

                    // ReSharper disable once RedundantArgumentDefaultValue

                    // we are the first in queue, so start the background updater
                    if (_pendingStates.Count == 1)
                        _ = Task.Run(() => UpdateStateAsync(timeToWait, default), cancellationToken);
                }

            // otherwise update immediately
            else
                await base.UpdateMessageAsync(context, message, embed, cancellationToken);

            _lastUpdateTime = currentTime;
        }

        async Task UpdateStateAsync(TimeSpan initialDelay,
                                    CancellationToken cancellationToken = default)
        {
            await Task.Delay(initialDelay, cancellationToken);

            var state = new InteractiveViewState(null,
                                                 Optional<string>.Unspecified,
                                                 Optional<Embed>.Unspecified);

            lock (_pendingStates)
            {
                // merge pending states into one
                while (_pendingStates.TryDequeue(out var s))
                {
                    state.Context = s.Context;

                    if (s.Message.IsSpecified)
                        state.Message = s.Message;

                    if (s.Embed.IsSpecified)
                        state.Embed = s.Embed;
                }
            }

            await base.UpdateMessageAsync(state.Context, state.Message, state.Embed, cancellationToken);
        }

        public virtual void Dispose() => _semaphore.Dispose();
    }
}