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
        public IReadOnlyDictionary<IEmote, IReactionTrigger> Triggers { get; private set; }

        protected abstract IEnumerable<IReactionTrigger> CreateTriggers();

        public override async Task<bool> UpdateViewAsync(IServiceProvider services,
            CancellationToken cancellationToken = default)
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

        public virtual void Dispose()
        {
        }
    }
}