using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
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
        readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public IReadOnlyDictionary<IEmote, IReactionTrigger> Triggers { get; private set; }

        protected abstract IEnumerable<IReactionTrigger> CreateTriggers();

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
                // message may have been deleted
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
                _semaphore.Release();
            }
        }

        public virtual void Dispose()
        {
            _semaphore.Dispose();
        }
    }
}