using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nhitomi.Models.Gateway;
using StackExchange.Redis;

namespace nhitomi.Database
{
    public class RedisMessageTransport : IMessageTransport
    {
        readonly IRedisClient _client;
        readonly IOptionsMonitor<RedisOptions> _options;
        readonly ILogger<RedisMessageTransport> _logger;

        public RedisMessageTransport(IRedisClient client, IOptionsMonitor<RedisOptions> options, ILogger<RedisMessageTransport> logger)
        {
            _client  = client;
            _options = options;
            _logger  = logger;
        }

        ISubscriber Subscriber => _client.ConnectionMultiplexer.GetSubscriber();

        readonly SemaphoreSlim _readerSemaphore = new SemaphoreSlim(1);
        readonly Dictionary<string, ChannelReader> _readers = new Dictionary<string, ChannelReader>();

        async Task<AwaitableQueue<MessageBase>> CreateQueueAsync(string channelName, CancellationToken cancellationToken = default)
        {
            await _readerSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (!_readers.TryGetValue(channelName, out var reader))
                {
                    // subscribe and create reader if not exist
                    var channel = await Subscriber.SubscribeAsync(channelName);

                    _logger.LogDebug($"Subscribed to channel: {channelName}");

                    reader = _readers[channelName] = new ChannelReader(channel, _logger);
                }

                var queue = new AwaitableQueue<MessageBase>();

                lock (reader.Queues)
                    reader.Queues.Add(queue);

                return queue;
            }
            finally
            {
                _readerSemaphore.Release();
            }
        }

        async Task DestroyQueueAsync(string channelName, AwaitableQueue<MessageBase> queue, CancellationToken cancellationToken = default)
        {
            await _readerSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (!_readers.TryGetValue(channelName, out var reader))
                    return;

                bool unsubscribe;

                lock (reader.Queues)
                {
                    reader.Queues.Remove(queue);

                    // unsubscribe if no queues left
                    unsubscribe = reader.Queues.Count == 0;
                }

                if (unsubscribe)
                {
                    _readers.Remove(channelName);

                    reader.Dispose();

                    await reader.Channel.UnsubscribeAsync();

                    _logger.LogDebug($"Unsubscribed from channel: {channelName}");
                }

                queue.Dispose();
            }
            finally
            {
                _readerSemaphore.Release();
            }
        }

        // redis multiplexes a single connection for all channel subscriptions
        // which means every message in a channel will be spread across multiple subscribers.
        // instead, we will use one "reader" instance that reads all messages in a channel
        // and duplicate them for all subscribers to receive.
        sealed class ChannelReader : IDisposable
        {
            readonly CancellationTokenSource _cts = new CancellationTokenSource();

            public readonly ChannelMessageQueue Channel;
            public readonly List<AwaitableQueue<MessageBase>> Queues = new List<AwaitableQueue<MessageBase>>();

            public ChannelReader(ChannelMessageQueue channel, ILogger logger)
            {
                Channel = channel;

                var cancellationToken = _cts.Token;

                Task.Run(async () =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var read = await Channel.ReadAsync(cancellationToken);

                        var message = MessagePackSerializer.Deserialize<MessageBase>(read.Message);

                        lock (Queues)
                        {
                            if (logger.IsEnabled(LogLevel.Debug))
                                logger.LogDebug($"Message {message.GetType().Name} in channel {Channel.Channel}, writing to {Queues.Count} queues.");

                            foreach (var queue in Queues)
                                queue.Enqueue(message);
                        }
                    }
                }, cancellationToken);
            }

            public void Dispose()
            {
                _cts.Cancel();
                _cts.Dispose();
            }
        }

        public IMessageQueue GetQueue(string name) => new MessageQueue(this, _options.CurrentValue.KeyPrefix + name);

        class MessageQueue : IMessageQueue
        {
            readonly RedisMessageTransport _transport;

            public string Name { get; }

            public MessageQueue(RedisMessageTransport transport, string name)
            {
                _transport = transport;

                Name = name;
            }

            public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) => GetQueueAsync(cancellationToken);

            AwaitableQueue<MessageBase> _queue;

            async Task<AwaitableQueue<MessageBase>> GetQueueAsync(CancellationToken cancellationToken = default)
                => _queue ??= await _transport.CreateQueueAsync(Name, cancellationToken);

            public Task EnqueueAsync(MessageBase message, CancellationToken cancellationToken = default)
                => _transport.Subscriber.PublishAsync(Name, MessagePackSerializer.Serialize(message));

            public async Task<MessageBase> DequeueAsync(CancellationToken cancellationToken = default)
            {
                var queue = await GetQueueAsync(cancellationToken);

                return await queue.DequeueAsync(cancellationToken);
            }

            public async ValueTask DisposeAsync()
            {
                var queue = _queue;

                if (queue != null)
                    await _transport.DestroyQueueAsync(Name, queue);
            }
        }

        public void Dispose() => _readerSemaphore.Dispose();
    }
}