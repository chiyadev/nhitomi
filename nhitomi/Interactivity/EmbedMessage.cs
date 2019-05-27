using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace nhitomi.Interactivity
{
    public abstract class EmbedMessage
    {
        public IServiceProvider Services { get; private set; }
        public ICommandContext Context { get; private set; }
        public IUserMessage Message { get; private set; }

        public virtual async Task<bool> InitializeAsync(IServiceProvider services, ICommandContext context,
            CancellationToken cancellationToken = default)
        {
            Services = services;
            Context = context;

            // initialize the view
            if (!await InitializeViewAsync(cancellationToken))
                return false;

            if (Message != null)
                throw new InvalidOperationException($"{GetType().Name} did not initialize its initial view.");

            return true;
        }

        protected abstract Task<bool> InitializeViewAsync(CancellationToken cancellationToken = default);

        protected async Task SetViewAsync(Embed embed, CancellationToken cancellationToken = default)
        {
            if (Message == null)
                Message = await Context.Channel.SendMessageAsync(embed: embed);
            else
                await Message.ModifyAsync(m => m.Embed = embed);
        }
    }
}