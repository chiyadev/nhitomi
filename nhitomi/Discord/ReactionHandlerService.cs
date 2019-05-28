using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using nhitomi.Interactivity;

namespace nhitomi.Discord
{
    public interface IReactionHandler
    {
        Task InitializeAsync(CancellationToken cancellationToken = default);

        Task<bool> TryHandleAsync(ReactionContext context, CancellationToken cancellationToken = default);
    }

    public class ReactionContext : ICommandContext
    {
        public IDiscordClient Client { get; }
        public IUserMessage Message { get; }
        public IUser User { get; }
        public IReaction Reaction { get; }
        public ReactionEvent Event { get; }

        IGuild ICommandContext.Guild => Message.Channel is IGuildChannel c ? c.Guild : null;
        IMessageChannel ICommandContext.Channel => Message.Channel;

        public ReactionContext(IDiscordClient discord, IUserMessage message, IReaction reaction, IUser user,
            ReactionEvent eventType)
        {
            Client = discord;
            Message = message;
            Reaction = reaction;
            User = user;
            Event = eventType;
        }
    }

    public enum ReactionEvent
    {
        Add,
        Remove
    }

    public class ReactionHandlerService : IHostedService
    {
        readonly DiscordService _discord;
        readonly ILogger<ReactionHandlerService> _logger;

        readonly IReactionHandler[] _reactionHandlers;

        [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
        public ReactionHandlerService(DiscordService discord, ILogger<ReactionHandlerService> logger,
            InteractiveManager interactiveManager)
        {
            _discord = discord;
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

                        var context = new ReactionContext(_discord, message, reaction, user, eventType);

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
    }
}