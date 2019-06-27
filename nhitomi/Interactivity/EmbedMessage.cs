using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using nhitomi.Discord;
using nhitomi.Globalization;

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

            protected async Task SetMessageAsync(string localizationKey,
                                                 object variables = null,
                                                 CancellationToken cancellationToken = default)
            {
                var path = new LocalizationPath(localizationKey);
                var l    = Context.GetLocalization();

                if (Message.Message == null)
                    Message.Message = await Context.Channel.SendMessageAsync(path[l, variables]);
                else
                    await Message.Message.ModifyAsync(m => m.Content = path[l, variables]);
            }

            protected async Task SetEmbedAsync(Embed embed,
                                               CancellationToken cancellationToken = default)
            {
                if (Message.Message == null)
                    Message.Message = await Context.Channel.SendMessageAsync(embed: embed);
                else
                    await Message.Message.ModifyAsync(m =>
                    {
                        m.Embed   = embed;
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