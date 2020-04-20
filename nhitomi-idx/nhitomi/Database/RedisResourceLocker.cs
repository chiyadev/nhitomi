using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace nhitomi.Database
{
    /// <summary>
    /// Implements a simple distributed lock based on Redis.
    /// This DOES NOT implement the algorithm described at https://redis.io/topics/distlock!
    /// </summary>
    public class RedisResourceLocker : IResourceLocker
    {
        readonly IRedisClient _client;
        readonly IOptionsMonitor<RedisOptions> _options;

        public RedisResourceLocker(IRedisClient client, IOptionsMonitor<RedisOptions> options)
        {
            _client  = client;
            _options = options;
        }

        public async Task<IAsyncDisposable> EnterAsync(string key, CancellationToken cancellationToken = default)
        {
            var locker = new Locker($"lock:{key}", _client, _options.CurrentValue.LockExpiry);

            await locker.AcquireAsync(cancellationToken);

            return locker;
        }

        sealed class Locker : IAsyncDisposable
        {
            readonly string _key;
            readonly IRedisClient _client;
            readonly TimeSpan _expire;

            readonly RedisValue _id = Guid.NewGuid().ToByteArray();
            readonly CancellationTokenSource _cts = new CancellationTokenSource();

            public Locker(string key, IRedisClient client, TimeSpan expire)
            {
                _key    = key;
                _client = client;
                _expire = expire;
            }

            volatile int _acquired;

            public async Task AcquireAsync(CancellationToken cancellationToken = default)
            {
                while (!await _client.SetAsync(_key, _id, _expire, When.NotExists, cancellationToken))
                    await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);

                Interlocked.Exchange(ref _acquired, 1);

                StartExtension(_cts.Token);
            }

            void StartExtension(CancellationToken cancellationToken = default) => Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(_expire / 2, cancellationToken);

                    if (!await _client.SetIfEqualAsync(_key, _id, _id, _expire, When.Exists, cancellationToken))
                        break; // handle this?
                }
            }, cancellationToken);

            public async ValueTask DisposeAsync()
            {
                if (Interlocked.CompareExchange(ref _acquired, 0, 1) == 0)
                    return;

                await _client.DeleteAsync(_key);

                _cts.Cancel();
                _cts.Dispose();
            }
        }

        public void Dispose() { }
    }

    [Serializable]
    public class RedisResourceLockerException : Exception
    {
        public RedisResourceLockerException() { }
        public RedisResourceLockerException(string message) : base(message) { }
        public RedisResourceLockerException(string message, Exception inner) : base(message, inner) { }

        protected RedisResourceLockerException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}