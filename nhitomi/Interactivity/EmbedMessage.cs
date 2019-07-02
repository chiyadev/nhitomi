using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using nhitomi.Discord;

namespace nhitomi.Interactivity
{
    public interface IEmbedMessage
    {
        /// <summary>
        /// The user which caused this interactive to be sent i.e. the author of a command.
        /// </summary>
        IUser Source { get; }

        /// <summary>
        /// The message this interactive is operating on.
        /// </summary>
        IUserMessage Message { get; }

        Task<bool> UpdateViewAsync(IServiceProvider services,
                                   CancellationToken cancellationToken = default);
    }

    public abstract class EmbedMessage<TView> : IEmbedMessage
        where TView : EmbedMessage<TView>.ViewBase
    {
        public IUser Source { get; private set; }
        public IUserMessage Message { get; private set; }

        static readonly DependencyFactory<TView> _viewFactory = DependencyUtility<TView>.Factory;

        public virtual Task<bool> UpdateViewAsync(IServiceProvider services,
                                                  CancellationToken cancellationToken = default)
        {
            // create view object
            var view = _viewFactory(services);
            view.Message = this;
            view.Context = services.GetRequiredService<IDiscordContext>();

            Source = view.Context.User;

            // update the view
            return view.UpdateAsync(cancellationToken);
        }

        public abstract class ViewBase
        {
            public EmbedMessage<TView> Message { get; set; }
            public IDiscordContext Context { get; set; }

            public abstract Task<bool> UpdateAsync(CancellationToken cancellationToken = default);

            protected Task SetMessageAsync(string localizationKey,
                                           object args = null,
                                           CancellationToken cancellationToken = default)
            {
                string content = Context.GetLocalization()[localizationKey, args];

                return SetViewAsync((Optional<string>) content, Optional<Embed>.Unspecified, cancellationToken);
            }

            protected Task SetEmbedAsync(Embed embed,
                                         CancellationToken cancellationToken = default) =>
                SetViewAsync(null, embed, cancellationToken);

            protected virtual async Task SetViewAsync(Optional<string> message,
                                                      Optional<Embed> embed,
                                                      CancellationToken cancellationToken = default)
            {
                if (Message.Message == null)
                    Message.Message = await Context.Channel.SendMessageAsync(message.GetValueOrDefault(),
                                                                             false,
                                                                             embed.GetValueOrDefault());
                else
                    await Message.Message.ModifyAsync(m =>
                    {
                        m.Content = message;
                        m.Embed   = embed;
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