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
    public interface IMessageHandler
    {
        Task InitializeAsync(CancellationToken cancellationToken = default);

        Task<bool> TryHandleAsync(IMessageContext context, CancellationToken cancellationToken = default);
    }

    public interface IMessageContext : IDiscordContext
    {
        MessageEvent Event { get; }
    }

    public enum MessageEvent
    {
        Create,
        Modify,
        Delete
    }

    public class MessageHandlerService : IHostedService
    {
        readonly DiscordService _discord;
        readonly GuildSettingsCache _guildSettingsCache;
        readonly DiscordErrorReporter _errorReporter;
        readonly ILogger<MessageHandlerService> _logger;

        readonly IMessageHandler[] _messageHandlers;

        [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
        public MessageHandlerService(DiscordService discord, GuildSettingsCache guildSettingsCache,
            DiscordErrorReporter errorReporter, ILogger<MessageHandlerService> logger, CommandExecutor commandExecutor,
            GalleryUrlDetector galleryUrlDetector, InteractiveManager interactiveManager)
        {
            _discord = discord;
            _guildSettingsCache = guildSettingsCache;
            _errorReporter = errorReporter;
            _logger = logger;

            _messageHandlers = new IMessageHandler[]
            {
                commandExecutor,
                galleryUrlDetector,
                interactiveManager
            };
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _discord.WaitForReadyAsync(cancellationToken);

            await Task.WhenAll(_messageHandlers.Select(h => h.InitializeAsync(cancellationToken)));

            _discord.MessageReceived += MessageReceived;
            _discord.MessageUpdated += MessageUpdated;
            _discord.MessageDeleted += MessageDeleted;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _discord.MessageReceived -= MessageReceived;
            _discord.MessageUpdated -= MessageUpdated;

            return Task.CompletedTask;
        }

        Task MessageReceived(SocketMessage message) =>
            HandleMessageAsync(message, MessageEvent.Create);

        Task MessageUpdated(Cacheable<IMessage, ulong> _, SocketMessage message, IMessageChannel channel) =>
            HandleMessageAsync(message, MessageEvent.Modify);

        Task MessageDeleted(Cacheable<IMessage, ulong> cacheable, ISocketMessageChannel channel) =>
            cacheable.HasValue
                ? HandleMessageAsync(cacheable.Value, MessageEvent.Delete)
                : Task.CompletedTask;

        public readonly AtomicCounter HandledMessages = new AtomicCounter();
        public readonly AtomicCounter ReceivedMessages = new AtomicCounter();

        Task HandleMessageAsync(IMessage socketMessage, MessageEvent eventType)
        {
            if (socketMessage is IUserMessage message &&
                socketMessage.Author.Id != _discord.CurrentUser.Id)
            {
                // handle on another thread to not block the gateway thread
                _ = Task.Run(async () =>
                {
                    // create context
                    var context = new MessageContext
                    {
                        Client = _discord,
                        Message = message,
                        Event = eventType,
                        GuildSettings = _guildSettingsCache[message.Channel]
                    };

                    try
                    {
                        foreach (var handler in _messageHandlers)
                            if (await handler.TryHandleAsync(context))
                            {
                                HandledMessages.Increment();
                                break;
                            }
                    }
                    catch (Exception e)
                    {
                        await _errorReporter.ReportAsync(e, context);
                    }
                    finally
                    {
                        ReceivedMessages.Increment();
                    }
                });
            }

            return Task.CompletedTask;
        }

        class MessageContext : IMessageContext
        {
            public IDiscordClient Client { get; set; }
            public IUserMessage Message { get; set; }
            public IMessageChannel Channel => Message.Channel;
            public IUser User => Message.Author;
            public Guild GuildSettings { get; set; }

            public MessageEvent Event { get; set; }
        }
    }
}