using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using nhitomi.Interactivity.Triggers;

namespace nhitomi.Interactivity
{
    public abstract class InteractiveMessage : IDisposable
    {
        public IServiceProvider Services { get; private set; }
        public ICommandContext Context { get; private set; }
        public IUserMessage Message { get; private set; }

        public IReadOnlyDictionary<IEmote, ReactionTrigger> Triggers { get; private set; }

        protected InteractiveMessage()
        {
            Triggers = CreateTriggers().ToDictionary(t => t.Emote);
        }

        protected abstract IEnumerable<ReactionTrigger> CreateTriggers();

        public async Task InitializeAsync(IServiceProvider services, ICommandContext context,
            CancellationToken cancellationToken = default)
        {
            Services = services;
            Context = context;

            // update view
            // this also sends the initial message
            await UpdateViewAsync(cancellationToken);

            if (Message == null)
                throw new InvalidOperationException($"{GetType().Name} did not initialize its initial view.");

            // initialize reaction triggers
            Triggers = CreateTriggers().ToDictionary(t => t.Emote);

            foreach (var trigger in Triggers.Values)
                trigger.Initialize(this);

            await Message.AddReactionsAsync(Triggers.Keys.ToArray());
        }

        protected abstract Task UpdateViewAsync(CancellationToken cancellationToken = default);

        protected async Task SetViewAsync(Embed embed, CancellationToken cancellationToken = default)
        {
            if (Message == null)
                Message = await Context.Channel.SendMessageAsync(embed: embed);
            else
                await Message.ModifyAsync(m => m.Embed = embed);
        }

        public virtual void Dispose()
        {
        }
    }
}