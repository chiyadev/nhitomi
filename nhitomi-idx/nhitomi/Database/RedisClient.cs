using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prometheus;
using StackExchange.Redis;

namespace nhitomi.Database
{
    public interface IRedisClient : IDisposable
    {
        Task InitializeAsync(CancellationToken cancellationToken = default);

        Task<byte[]> GetAsync(RedisKey key, CancellationToken cancellationToken = default);
        Task<byte[][]> GetManyAsync(RedisKey[] keys, CancellationToken cancellationToken = default);
        Task<T> GetObjectAsync<T>(RedisKey key, CancellationToken cancellationToken = default);
        Task<T[]> GetObjectManyAsync<T>(RedisKey[] keys, CancellationToken cancellationToken = default);
        Task<long> GetIntegerAsync(RedisKey key, CancellationToken cancellationToken = default);

        Task<bool> SetAsync(RedisKey key, RedisValue value, TimeSpan? expiry = null, When when = When.Always, CancellationToken cancellationToken = default);
        Task<bool> SetManyAsync(RedisKey[] keys, RedisValue[] values, When when = When.Always, CancellationToken cancellationToken = default);
        Task<bool> SetObjectAsync<T>(RedisKey key, T value, TimeSpan? expiry = null, When when = When.Always, CancellationToken cancellationToken = default);
        Task<bool> SetObjectManyAsync<T>(RedisKey[] keys, T[] values, TimeSpan? expiry = null, When when = When.Always, CancellationToken cancellationToken = default);
        Task<bool> SetIfEqualAsync(RedisKey key, RedisValue value, RedisValue comparand, TimeSpan? expiry = null, When when = When.Always, CancellationToken cancellationToken = default);
        Task<long> IncrementIntegerAsync(RedisKey key, long delta, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(RedisKey key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes all keys created by this client. This is useful for unit testing.
        /// </summary>
        Task ResetAsync(CancellationToken cancellationToken = default);
    }

    public class RedisOptions
    {
        /// <summary>
        /// Redis instance endpoint.
        /// </summary>
        public string Endpoint { get; set; } = "localhost:6379";

        /// <summary>
        /// Prefix to use for every key and channel name in Redis.
        /// </summary>
        public string KeyPrefix { get; set; }

        /// <summary>
        /// Time before automatic lock expiry when using Redis for distributed locks.
        /// </summary>
        public TimeSpan LockExpiry { get; set; } = TimeSpan.FromMinutes(1);
    }

    public class RedisClient : IRedisClient
    {
        static readonly MessagePackSerializerOptions _serializerOptions = ContractlessStandardResolver.Options;

        readonly IOptionsMonitor<RedisOptions> _options;
        readonly ILogger<RedisClient> _logger;

        readonly RedisKey _keyPrefix;
        readonly RedisKeyMemory _keyMemory;

        public RedisClient(IOptionsMonitor<RedisOptions> options, ILogger<RedisClient> logger, IHostEnvironment environment)
        {
            _options = options;
            _logger  = logger;

            _keyPrefix = _options.CurrentValue.KeyPrefix; // key prefix is cached for performance
            _keyMemory = environment.IsDevelopment() ? new RedisKeyMemory() : null;
        }

        ConnectionMultiplexer _connection;
        IDatabase _database;

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            var options = _options.CurrentValue;

            _logger.LogWarning($"Connecting to Redis endpoint: {options.Endpoint}");

            _connection = await ConnectionMultiplexer.ConnectAsync(options.Endpoint);
            _database   = _connection.GetDatabase();
        }

        static readonly Histogram _requestTime = Metrics.CreateHistogram("redis_request_time", "Time spent on making Redis requests.", new HistogramConfiguration
        {
            Buckets = HistogramEx.ExponentialBuckets(0.1, 1000, 15)
        });

        public async Task<byte[]> GetAsync(RedisKey key, CancellationToken cancellationToken = default)
        {
            _keyMemory?.Add(key);

            using (_requestTime.Measure())
                return await _database.StringGetAsync(key.Prepend(_keyPrefix));
        }

        public async Task<byte[][]> GetManyAsync(RedisKey[] keys, CancellationToken cancellationToken = default)
        {
            _keyMemory?.Add(keys);

            var keys2 = new RedisKey[keys.Length];

            for (var i = 0; i < keys.Length; i++)
                keys2[i] = keys[i].Prepend(_keyPrefix);

            RedisValue[] values;

            using (_requestTime.Measure())
                values = await _database.StringGetAsync(keys2);

            var values2 = new byte[values.Length][];

            for (var i = 0; i < values.Length; i++)
                values2[i] = values[i];

            return values2;
        }

        public async Task<T> GetObjectAsync<T>(RedisKey key, CancellationToken cancellationToken = default)
        {
            var buffer = await GetAsync(key, cancellationToken);

            if (buffer == null)
                return default;

            return MessagePackSerializer.Deserialize<T>(buffer, _serializerOptions);
        }

        public async Task<T[]> GetObjectManyAsync<T>(RedisKey[] keys, CancellationToken cancellationToken = default)
        {
            var buffers = await GetManyAsync(keys, cancellationToken);
            var values  = new T[buffers.Length];

            for (var i = 0; i < buffers.Length; i++)
            {
                var buffer = buffers[i];

                if (buffer == null)
                    continue;

                values[i] = MessagePackSerializer.Deserialize<T>(buffer, _serializerOptions);
            }

            return values;
        }

        public async Task<long> GetIntegerAsync(RedisKey key, CancellationToken cancellationToken = default)
        {
            _keyMemory?.Add(key);

            using (_requestTime.Measure())
                return (long) await _database.StringGetAsync(key.Prepend(_keyPrefix));
        }

        public async Task<bool> SetAsync(RedisKey key, RedisValue value, TimeSpan? expiry = null, When when = When.Always, CancellationToken cancellationToken = default)
        {
            _keyMemory?.Add(key);

            using (_requestTime.Measure())
                return await _database.StringSetAsync(key.Prepend(_keyPrefix), value, expiry, when);
        }

        public async Task<bool> SetManyAsync(RedisKey[] keys, RedisValue[] values, When when = When.Always, CancellationToken cancellationToken = default)
        {
            _keyMemory?.Add(keys);

            var pairs = new KeyValuePair<RedisKey, RedisValue>[keys.Length];

            for (var i = 0; i < keys.Length; i++)
                pairs[i] = new KeyValuePair<RedisKey, RedisValue>(keys[i].Prepend(_keyPrefix), values[i]);

            using (_requestTime.Measure())
                return await _database.StringSetAsync(pairs, when);
        }

        public Task<bool> SetObjectAsync<T>(RedisKey key, T value, TimeSpan? expiry = null, When when = When.Always, CancellationToken cancellationToken = default)
            => SetAsync(key, MessagePackSerializer.Serialize(value, _serializerOptions), expiry, when, cancellationToken);

        public async Task<bool> SetObjectManyAsync<T>(RedisKey[] keys, T[] values, TimeSpan? expiry = null, When when = When.Always, CancellationToken cancellationToken = default)
        {
            _keyMemory?.Add(keys);

            var pairs = new KeyValuePair<RedisKey, RedisValue>[keys.Length];

            for (var i = 0; i < keys.Length; i++)
                pairs[i] = new KeyValuePair<RedisKey, RedisValue>(keys[i].Prepend(_keyPrefix), MessagePackSerializer.Serialize(values[i], _serializerOptions));

            var transaction = _database.CreateTransaction();

            var set = transaction.StringSetAsync(pairs, when);

            if (expiry != null)
                foreach (var pair in pairs)
                {
                    var _ = transaction.KeyExpireAsync(pair.Key, expiry);
                }

            using (_requestTime.Measure())
                return await transaction.ExecuteAsync() && await set;
        }

        public async Task<bool> SetIfEqualAsync(RedisKey key, RedisValue value, RedisValue comparand, TimeSpan? expiry = null, When when = When.Always, CancellationToken cancellationToken = default)
        {
            _keyMemory?.Add(key);
            key = key.Prepend(_keyPrefix);

            var transaction = _database.CreateTransaction();

            transaction.AddCondition(Condition.StringEqual(key, comparand));

            var set = transaction.StringSetAsync(key, value, expiry, when);

            using (_requestTime.Measure())
                return await transaction.ExecuteAsync() && await set;
        }

        public async Task<long> IncrementIntegerAsync(RedisKey key, long delta, CancellationToken cancellationToken = default)
        {
            _keyMemory?.Add(key);

            using (_requestTime.Measure())
                return await _database.StringIncrementAsync(key.Prepend(_keyPrefix), delta);
        }

        public async Task<bool> DeleteAsync(RedisKey key, CancellationToken cancellationToken = default)
        {
            _keyMemory?.Add(key);

            using (_requestTime.Measure())
                return await _database.KeyDeleteAsync(key.Prepend(_keyPrefix));
        }

        public async Task ResetAsync(CancellationToken cancellationToken = default)
        {
            if (_keyMemory == null)
                return;

            var keys = _keyMemory.Clear(_keyPrefix);

            if (keys.Length == 0)
                return;

            using (_requestTime.Measure())
                await _database.KeyDeleteAsync(keys);

            _logger.LogDebug($"Deleted all known {keys.Length} keys.");
        }

        public void Dispose()
        {
            _connection?.Dispose();
            _keyMemory?.Dispose();
        }
    }
}