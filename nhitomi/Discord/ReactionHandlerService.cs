using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using nhitomi.Globalization;
using nhitomi.Interactivity;

namespace nhitomi.Discord
{
    public interface IReactionHandler
    {
        Task InitializeAsync(CancellationToken cancellationToken = default);

        Task<bool> TryHandleAsync(IReactionContext context, CancellationToken cancellationToken = default);
    }

    public interface IReactionContext : IDiscordContext
    {
        IReaction Reaction { get; }
        ReactionEvent Event { get; }
    }

    public enum ReactionEvent
    {
        Add,
        Remove
    }

    public class ReactionHandlerService : IHostedService
    {
        readonly DiscordService _discord;
        readonly LocalizationCache _localizationCache;
        readonly ILogger<ReactionHandlerService> _logger;

        readonly IReactionHandler[] _reactionHandlers;

        [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
        public ReactionHandlerService(DiscordService discord, LocalizationCache localizationCache,
            ILogger<ReactionHandlerService> logger, InteractiveManager interactiveManager)
        {
            _discord = discord;
            _localizationCache = localizationCache;
            _logger = logger;

            _reactionHandlers = new IReactionHandler[]
            {
                interactiveManager
            };
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.WhenAll(_reactionHandlers.Select(h => h.InitializeAsync(cancellationToken)));

            _discord.ReactionAdded += ReactionAdded;
            _discord.ReactionRemoved += ReactionRemoved;
        }


        public Task StopAsync(CancellationToken cancellationToken)
        {
            _discord.ReactionAdded -= ReactionAdded;
            _discord.ReactionRemoved -= ReactionRemoved;

            return Task.CompletedTask;
        }

        Task ReactionAdded(Cacheable<IUserMessage, ulong> _, IMessageChannel channel, SocketReaction reaction) =>
            HandleReactionAsync(channel, reaction, ReactionEvent.Add);

        Task ReactionRemoved(Cacheable<IUserMessage, ulong> _, IMessageChannel channel, SocketReaction reaction) =>
            HandleReactionAsync(channel, reaction, ReactionEvent.Remove);

        Task HandleReactionAsync(IMessageChannel channel, SocketReaction reaction, ReactionEvent eventType)
        {
            if (reaction.UserId != _discord.CurrentUser.Id)
            {
                // handle on another thread to not block the gateway thread
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // retrieve message
                        var message = reaction.Message.IsSpecified
                            ? reaction.Message.Value
                            : (IUserMessage) await channel.GetMessageAsync(reaction.MessageId);

                        // retrieve user
                        var user = reaction.User.IsSpecified
                            ? reaction.User.Value
                            : await channel.GetUserAsync(reaction.UserId);

                        // create context
                        var context = new ReactionContext
                        {
                            Client = _discord,
                            Message = message,
                            User = user,
                            Reaction = reaction,
                            Event = eventType
                        };
                        context.Localization = _localizationCache[context];

                        foreach (var handler in _reactionHandlers)
                            if (await handler.TryHandleAsync(context))
                                return;
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, "Unhandled exception while handling message.");
                    }
                });
            }

            return Task.CompletedTask;
        }

        class ReactionContext : IReactionContext
        {
            public IDiscordClient Client { get; set; }
            public IUserMessage Message { get; set; }
            public IMessageChannel Channel => Message.Channel;
            public IUser User { get; set; }
            public Localization Localization { get; set; }

            public IReaction Reaction { get; set; }
            public ReactionEvent Event { get; set; }
        }
    }
}