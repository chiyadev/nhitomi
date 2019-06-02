using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using nhitomi.Discord;

namespace nhitomi.Interactivity
{
    public interface IEmbedMessage
    {
        IUserMessage Message { get; }

        Task<bool> UpdateViewAsync(IServiceProvider services, IDiscordContext context,
            CancellationToken cancellationToken = default);
    }

    public abstract class EmbedMessage<TView> : IEmbedMessage
        where TView : EmbedMessage<TView>.ViewBase
    {
        public IUserMessage Message { get; private set; }

        static readonly DependencyFactory<TView> _viewFactory = DependencyUtility<TView>.Factory;

        public virtual Task<bool> UpdateViewAsync(IServiceProvider services, IDiscordContext context,
            CancellationToken cancellationToken = default)
        {
            // create view object
            var view = _viewFactory(services);
            view.Context = context;
            view.Message = this;

            // update the view
            return view.UpdateAsync(cancellationToken);
        }

        public abstract class ViewBase
        {
            public EmbedMessage<TView> Message { get; set; }
            public IDiscordContext Context { get; set; }

            public abstract Task<bool> UpdateAsync(CancellationToken cancellationToken = default);

            protected async Task SetMessageAsync(string content, CancellationToken cancellationToken = default)
            {
                if (Message.Message == null)
                    Message.Message = await Context.Channel.SendMessageAsync(content);
                else
                    await Message.Message.ModifyAsync(m => m.Content = content ?? "");
            }

            protected async Task SetEmbedAsync(Embed embed, CancellationToken cancellationToken = default)
            {
                if (Message.Message == null)
                    Message.Message = await Context.Channel.SendMessageAsync(embed: embed);
                else
                    await Message.Message.ModifyAsync(m =>
                    {
                        m.Embed = embed;
                        m.Content = null;
                    });
            }
        }

        public abstract class EmbedViewBase : ViewBase
        {
            protected abstract Embed CreateEmbed();

            public override async Task<bool> UpdateAsync(CancellationToken cancellationToken = default)
            {
                await SetEmbedAsync(CreateEmbed(), cancellationToken);

                return true;
            }
        }
    }
}