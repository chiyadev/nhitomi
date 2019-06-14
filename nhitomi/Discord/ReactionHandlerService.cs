using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using nhitomi.Core;
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
        readonly GuildSettingsCache _guildSettingsCache;
        readonly DiscordErrorReporter _errorReporter;
        readonly ILogger<ReactionHandlerService> _logger;

        readonly IReactionHandler[] _reactionHandlers;

        [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
        public ReactionHandlerService(DiscordService discord, GuildSettingsCache guildSettingsCache,
            DiscordErrorReporter errorReporter, ILogger<ReactionHandlerService> logger,
            InteractiveManager interactiveManager)
        {
            _discord = discord;
            _guildSettingsCache = guildSettingsCache;
            _errorReporter = errorReporter;
            _logger = logger;

            _reactionHandlers = new IReactionHandler[]
            {
                interactiveManager
            };
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _discord.WaitForReadyAsync(cancellationToken);

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

        public readonly AtomicCounter HandledReactions = new AtomicCounter();
        public readonly AtomicCounter ReceivedReactions = new AtomicCounter();

        Task HandleReactionAsync(IMessageChannel channel, SocketReaction reaction, ReactionEvent eventType)
        {
            if (reaction.UserId != _discord.CurrentUser.Id)
            {
                // handle on another thread to not block the gateway thread
                _ = Task.Run(async () =>
                {
                    // retrieve message
                    if (!(await channel.GetMessageAsync(reaction.MessageId) is IUserMessage message))
                        return;

                    // retrieve user
                    if (!(await channel.GetUserAsync(reaction.UserId) is IUser user))
                        return;

                    // create context
                    var context = new ReactionContext
                    {
                        Client = _discord,
                        Message = message,
                        User = user,
                        GuildSettings = _guildSettingsCache[message.Channel],
                        Reaction = reaction,
                        Event = eventType
                    };

                    try
                    {
                        foreach (var handler in _reactionHandlers)
                            if (await handler.TryHandleAsync(context))
                            {
                                HandledReactions.Increment();
                                break;
                            }
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, "Unhandled exception while handling message.");

                        await _errorReporter.ReportAsync(e, context, false);
                    }
                    finally
                    {
                        ReceivedReactions.Increment();
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
            public Guild GuildSettings { get; set; }

            public IReaction Reaction { get; set; }
            public ReactionEvent Event { get; set; }
        }
    }
}