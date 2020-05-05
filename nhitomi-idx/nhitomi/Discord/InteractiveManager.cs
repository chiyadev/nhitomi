using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace nhitomi.Discord
{
    public interface IInteractiveManager : IAsyncDisposable
    {
        int Count { get; }
        TimeSpan InteractiveExpiry { get; }

        Task<bool> RegisterAsync(nhitomiCommandContext context, InteractiveMessage message, CancellationToken cancellationToken = default);
        Task UnregisterAsync(InteractiveMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds currently active interactive messages in a channel by a specific executor.
        /// </summary>
        IEnumerable<TMessage> Find<TMessage>(ulong channelId, ulong executorId) where TMessage : InteractiveMessage;

        /// <summary>
        /// Gets an interactive message by message ID.
        /// </summary>
        InteractiveMessage GetMessage(ulong messageId);

        /// <summary>
        /// Gets the trigger responsible for handling the given reaction emote on a message.
        /// </summary>
        ReactionTrigger GetTrigger(ulong messageId, IEmote emote);
    }

    /// <summary>
    /// Responsible for managing stateful reply messages.
    /// </summary>
    public class InteractiveManager : IInteractiveManager
    {
        readonly IOptionsMonitor<DiscordOptions> _options;
        readonly ILogger<InteractiveManager> _logger;

        public InteractiveManager(IOptionsMonitor<DiscordOptions> options, ILogger<InteractiveManager> logger)
        {
            _options = options;
            _logger  = logger;
        }

        public int Count
        {
            get
            {
                lock (_lock)
                    return _commandMap.Count;
            }
        }

        public TimeSpan InteractiveExpiry => _options.CurrentValue.Interactive.Expiry;

        readonly object _lock = new object();

        /// <summary>
        /// Maps command message ID to interactive.
        /// </summary>
        readonly Dictionary<ulong, InteractiveMessage> _commandMap = new Dictionary<ulong, InteractiveMessage>();

        /// <summary>
        /// Maps reply message ID to interactive.
        /// </summary>
        readonly Dictionary<ulong, InteractiveMessage> _replyMap = new Dictionary<ulong, InteractiveMessage>();

        /// <summary>
        /// Maps channel ID to interactive list.
        /// </summary>
        readonly Dictionary<ulong, List<InteractiveMessage>> _channelMap = new Dictionary<ulong, List<InteractiveMessage>>();

        public async Task<bool> RegisterAsync(nhitomiCommandContext context, InteractiveMessage message, CancellationToken cancellationToken = default)
        {
            if (!await message.InitializeAsync(this, context, cancellationToken))
                return false;

            lock (_lock)
            {
                _commandMap[message.Command.Id] = message;
                _replyMap[message.Reply.Id]     = message;

                _channelMap.TryAdd(message.Channel.Id, new List<InteractiveMessage>());
                _channelMap[message.Channel.Id].Add(message);
            }

            try
            {
                // add reaction triggers
                await message.Reply.AddReactionsAsync(message.Triggers.Keys.ToArray());
            }
            catch (Exception e)
            {
                _logger.LogDebug(e, $"Could not add reaction triggers to message {message}.");
            }

            return true;
        }

        public async Task UnregisterAsync(InteractiveMessage message, CancellationToken cancellationToken = default)
        {
            var result = false;

            lock (_lock)
            {
                result |= _commandMap.Remove(message.Command.Id);
                result |= _replyMap.Remove(message.Reply.Id);

                if (_channelMap.TryGetValue(message.Channel.Id, out var list))
                {
                    result |= list.Remove(message);

                    if (list.Count == 0)
                        _channelMap.Remove(message.Channel.Id);
                }
            }

            if (result)
            {
                _logger.LogDebug($"Destroying interactive message {message}");

                await message.DisposeAsyncInternal();
            }
        }

        public IEnumerable<TMessage> Find<TMessage>(ulong channelId, ulong executorId) where TMessage : InteractiveMessage
        {
            lock (_lock)
            {
                if (!_channelMap.TryGetValue(channelId, out var messages))
                    yield break;

                foreach (var message in messages)
                {
                    if (message is TMessage m && message.Command.Author.Id == executorId)
                        yield return m;
                }
            }
        }

        public InteractiveMessage GetMessage(ulong messageId)
        {
            lock (_lock)
            {
                return _replyMap.TryGetValue(messageId, out var message)
                    ? message
                    : null;
            }
        }

        public ReactionTrigger GetTrigger(ulong messageId, IEmote emote)
        {
            lock (_lock)
            {
                return _replyMap.TryGetValue(messageId, out var message) && message.Triggers.TryGetValue(emote, out var trigger)
                    ? trigger
                    : null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var message in _commandMap.Values.ToArray())
                await UnregisterAsync(message);
        }
    }
}