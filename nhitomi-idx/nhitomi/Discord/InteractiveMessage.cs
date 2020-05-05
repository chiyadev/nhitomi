using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;

namespace nhitomi.Discord
{
    /// <summary>
    /// Represents a reply message that interacts with the user outside of the command scope.
    /// An interactive message is not necessarily stateful.
    /// However when it is, consumers MUST lock this object using <see cref="Semaphore"/> before altering any state.
    /// </summary>
    public abstract class InteractiveMessage : ReplyMessage, IAsyncDisposable
    {
        /// <summary>
        /// Semaphore used to lock this message during all operations (except init and dispose).
        /// </summary>
        public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1);

        /// <summary>
        /// Creates reaction triggers for this interactive.
        /// </summary>
        protected virtual IEnumerable<ReactionTrigger> CreateTriggers() => Enumerable.Empty<ReactionTrigger>();

        nhitomiCommandContext _context;

        public nhitomiCommandContext Context => _context;

        /// <summary>
        /// Scope of services provided for this interactive.
        /// This scope is always used for rendering until this interactive expires.
        /// </summary>
        public IServiceScope ServiceScope { get; private set; }

        /// <summary>
        /// Services provided for this interactive.
        /// </summary>
        public IServiceProvider Services => ServiceScope.ServiceProvider;

        /// <summary>
        /// Message containing the command that caused this interactive to spawn.
        /// </summary>
        public IUserMessage Command => Context.Message;

        /// <summary>
        /// Message containing the rendered reply of this interactive.
        /// </summary>
        public IUserMessage Reply { get; private set; }

        /// <summary>
        /// Channel in which this interactive operates.
        /// </summary>
        public IMessageChannel Channel => Reply?.Channel ?? Command?.Channel;

        /// <summary>
        /// Renderer for this interactive.
        /// </summary>
        public InteractiveRenderer Renderer { get; private set; }

        /// <summary>
        /// Reaction triggers attached to this interactive.
        /// </summary>
        public IReadOnlyDictionary<IEmote, ReactionTrigger> Triggers { get; private set; }

        /// <summary>
        /// Timeout associated with this interactive.
        /// When timeout is reached, this interactive will be disposed.
        /// </summary>
        public Timeout Timeout { get; private set; }

        internal async Task<bool> InitializeAsync(IInteractiveManager manager, nhitomiCommandContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                if (Interlocked.CompareExchange(ref _context, context, null) != null)
                    throw new InvalidOperationException($"Interactive message {GetType()} already initialized.");

                ServiceScope = context.ServiceScope.CreateReference();
                Renderer     = ActivatorUtilities.CreateInstance<InteractiveRenderer>(Services, this);

                // create reaction triggers
                Triggers = CreateTriggers().ToDictionary(t => t.Emote);

                foreach (var trigger in Triggers.Values)
                    trigger.Initialize(this);

                // schedule timeout disposal
                Timeout = new Timeout(manager.InteractiveExpiry) { CompleteOnDisposal = true };

                var _ = Timeout.Task.ContinueWith(t => DisposeAsync(), TaskContinuationOptions.None);

                // initialize before initial render
                await OnInitialize(cancellationToken);

                // send static reply which will be modified with further renders
                Reply = await Renderer.RenderInitialAsync(cancellationToken);

                return Reply != null;
            }
            finally
            {
                if (Reply == null)
                    await DisposeAsyncInternal();
            }
        }

        /// <summary>
        /// Modifies the reply message of this interactive with newly rendered content.
        /// </summary>
        public Task RerenderAsync(CancellationToken cancellationToken = default)
        {
            if (_context == null)
                return Task.CompletedTask;

            return Renderer.RenderAsync(cancellationToken);
        }

        /// <summary>
        /// Asynchronously listens for a message from the user who commands this interactive.
        /// </summary>
        public Task<IUserMessage> ListenAsync(string message, CancellationToken cancellationToken = default)
            => Services.GetService<IDiscordMessageHandler>()
                       .ListenAsync(new MessageListenArgs
                        {
                            User    = Command.Author,
                            Channel = Channel,
                            Message = message
                        }, cancellationToken);

        public override string ToString() => $"{GetType().Name} {Reply?.Id.ToString() ?? "<not initialized>"}";

        public async ValueTask DisposeAsync()
            => await Services.GetService<IInteractiveManager>().UnregisterAsync(this); // manager calls internal dispose

        internal ValueTask DisposeAsyncInternal()
        {
            Semaphore.Dispose();

            ServiceScope?.Dispose();
            Renderer?.Dispose();
            Timeout?.Dispose();

            if (Triggers != null)
                foreach (var trigger in Triggers.Values)
                    trigger.Dispose();

            _context     = null;
            ServiceScope = null;
            Reply        = null;
            Renderer     = null;
            Triggers     = null;
            Timeout      = null;

            return OnDisposeAsync();
        }

        /// <summary>
        /// Called when this interactive is being initialized.
        /// This is called before initial render, so <see cref="Reply"/> is not available.
        /// </summary>
        protected virtual Task OnInitialize(CancellationToken cancellationToken = default) => Task.CompletedTask;

        /// <summary>
        /// Called when this interactive is being disposed.
        /// Message is not locked during dispose.
        /// </summary>
        protected virtual ValueTask OnDisposeAsync() => new ValueTask();
    }
}