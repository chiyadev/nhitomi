using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Prometheus;
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

        static readonly Gauge _acquireOngoing = Metrics.CreateGauge("locker_acquire_ongoing", "Number of locks in the process of being acquired.");
        static readonly Counter _acquires = Metrics.CreateCounter("locker_acquires", "Number of locks successfully acquired.");

        public async Task<IAsyncDisposable> EnterAsync(string key, CancellationToken cancellationToken = default)
        {
            var locker = new Locker($"lock:{key}", _client, _options.CurrentValue.LockExpiry);

            using (_acquireOngoing.TrackInProgress())
                await locker.AcquireAsync(cancellationToken);

            _acquires.Inc();

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

            static readonly Counter _contention = Metrics.CreateCounter("locker_contention", "Number of failed attempts to acquire a lock.");

            static readonly Histogram _time = Metrics.CreateHistogram("locker_time", "Time spent between lock acquire and release.", new HistogramConfiguration
            {
                Buckets = HistogramEx.ExponentialBuckets(0.01, 60, 20)
            });

            volatile IDisposable _acquired;

            public async Task AcquireAsync(CancellationToken cancellationToken = default)
            {
                for (var i = 1; !await _client.SetAsync(_key, _id, _expire, When.NotExists, cancellationToken); i++)
                {
                    _contention.Inc();

                    await Task.Delay(TimeSpan.FromMilliseconds(10 * Math.Clamp(i, 1, 10)), cancellationToken);
                }

                Interlocked.Exchange(ref _acquired, _time.Measure());

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

            public ValueTask DisposeAsync()
            {
                var acquired = Interlocked.Exchange(ref _acquired, null);

                if (acquired == null)
                    return default;

                acquired.Dispose();

                _cts.Cancel();
                _cts.Dispose();

                return new ValueTask(_client.DeleteAsync(_key));
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