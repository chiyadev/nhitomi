using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace nhitomi.Discord
{
    public interface IMessageHandler
    {
        Task InitializeAsync(CancellationToken cancellationToken = default);

        Task<bool> TryHandleAsync(MessageContext context, CancellationToken cancellationToken = default);
    }

    public class MessageContext
    {
        public IUserMessage Message { get; }
        public MessageEvent Event { get; }

        public MessageContext(IUserMessage message, MessageEvent @event)
        {
            Message = message;
            Event = @event;
        }
    }

    public enum MessageEvent
    {
        Create,
        Edit
    }

    public class MessageHandlerService : IHostedService
    {
        readonly DiscordService _discord;
        readonly ILogger<MessageHandlerService> _logger;

        readonly IMessageHandler[] _messageHandlers;

        [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
        public MessageHandlerService(DiscordService discord, ILogger<MessageHandlerService> logger,
            CommandExecutor commandExecutor,
            GalleryUrlDetector galleryUrlDetector)
        {
            _discord = discord;
            _logger = logger;

            _messageHandlers = new IMessageHandler[]
            {
                commandExecutor,
                galleryUrlDetector
            };
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.WhenAll(_messageHandlers.Select(h => h.InitializeAsync(cancellationToken)));

            _discord.Socket.MessageReceived += MessageReceived;
            _discord.Socket.MessageUpdated += MessageUpdated;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _discord.Socket.MessageReceived -= MessageReceived;
            _discord.Socket.MessageUpdated -= MessageUpdated;

            return Task.CompletedTask;
        }

        Task MessageReceived(SocketMessage message) =>
            HandleMessageAsync(message, MessageEvent.Create);

        Task MessageUpdated(Cacheable<IMessage, ulong> _, SocketMessage message, IMessageChannel channel) =>
            HandleMessageAsync(message, MessageEvent.Edit);

        Task HandleMessageAsync(SocketMessage message, MessageEvent eventType)
        {
            if (message is SocketUserMessage userMessage &&
                message.Author.Id != _discord.Socket.CurrentUser.Id)
            {
                // handle on another thread to not block the gateway thread
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var context = new MessageContext(userMessage, eventType);

                        foreach (var handler in _messageHandlers)
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