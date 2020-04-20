using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace nhitomi.Discord
{
    public class nhitomiCommandContext : CommandContext
    {
        /// <summary>
        /// Gets the ref-counted scope of <see cref="CommandContext.ServiceProvider"/>.
        /// </summary>
        public RefCountedServiceScope ServiceScope { get; }

        /// <summary>
        /// User who executed the command.
        /// </summary>
        public IUser Executor => Message.Author;

        /// <summary>
        /// Message that contains the command string.
        /// </summary>
        public IUserMessage Message { get; }

        /// <summary>
        /// Channel in which <see cref="Message"/> was sent.
        /// </summary>
        public IMessageChannel Channel => Message.Channel;

        /// <summary>
        /// Guild in which <see cref="Message"/> was sent. This can be null.
        /// </summary>
        public IGuild Guild => (Channel as IGuildChannel)?.Guild;

        /// <summary>
        /// Cancellation token.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        public nhitomiCommandContext(RefCountedServiceScope scope, IUserMessage message, CancellationToken cancellationToken = default) : base(scope.ServiceProvider)
        {
            ServiceScope      = scope;
            Message           = message;
            CancellationToken = cancellationToken;
        }

        IInteractiveManager InteractiveManager => ServiceProvider.GetService<IInteractiveManager>();
        IReplyRenderer ReplyRenderer => ServiceProvider.GetService<IReplyRenderer>();

        /// <summary>
        /// Finds the last interactive of the given type in the executor's command channel. This can return null.
        /// </summary>
        public TMessage FindInteractive<TMessage>() where TMessage : InteractiveMessage
            => InteractiveManager.Find<TMessage>(Channel.Id, Executor.Id).LastOrDefault();

        /// <inheritdoc cref="SendAsync{TMessage}(System.Action{TMessage},System.Threading.CancellationToken)"/>
        public Task SendAsync<TMessage>(CancellationToken cancellationToken = default) where TMessage : ReplyMessage
            => SendAsync<TMessage>(_ => { }, cancellationToken);

        /// <summary>
        /// Sends a generic reply message to <see cref="Executor"/> that is dynamically activated using dependencies.
        /// </summary>
        public Task SendAsync<TMessage>(Action<TMessage> configure, CancellationToken cancellationToken = default) where TMessage : ReplyMessage
        {
            var message = ActivatorUtilities.CreateInstance<TMessage>(ServiceProvider, this);

            configure?.Invoke(message);

            return SendAsync(message, cancellationToken);
        }

        /// <summary>
        /// Sends a reply message to <see cref="Executor"/>.
        /// </summary>
        public async Task SendAsync(ReplyMessage reply, CancellationToken cancellationToken = default)
        {
            // if interactive, register in interactive manager
            if (reply is InteractiveMessage interactiveReply)
                await InteractiveManager.RegisterAsync(Message, interactiveReply, ServiceScope.CreateReference(), cancellationToken);

            // else send statically
            else await ReplyRenderer.SendAsync(Message, reply, cancellationToken);
        }
    }
}