using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace nhitomi.Database
{
    public interface IRedisClient : IDisposable, IRedisHashHandler
    {
        ConnectionMultiplexer ConnectionMultiplexer { get; }

        Task InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a handler for the members of the specified hash.
        /// </summary>
        IRedisHashHandler Hash(RedisKey key);

        /// <summary>
        /// Returns a handler for the members of the specified set.
        /// </summary>
        IRedisSetHandler Set(RedisKey key);

        /// <summary>
        /// Returns a handler for the members of the specified sorted set.
        /// </summary>
        IRedisSortedSetHandler SortedSet(RedisKey key);

        /// <summary>
        /// Deletes all keys created by this client. This is useful for unit testing.
        /// </summary>
        Task ResetAsync(CancellationToken cancellationToken = default);
    }

    /// <remarks>Expiry parameters will be ignored for this handler.</remarks>
    public interface IRedisHashHandler
    {
        // don't use compression in redis for performance
        public static readonly MessagePackSerializerOptions DefaultObjectSerializerOptions = ContractlessStandardResolver.Options;

        async Task<(bool, T)> GetObjectAsync<T>(RedisKey key, CancellationToken cancellationToken = default) where T : class
        {
            var buffer = await GetAsync(key, cancellationToken);

            return buffer switch
            {
                // did not exist
                null => (false, default),

                // exists but is null
                {Length: 0} => (true, default),

                _ => (true, MessagePackSerializer.Deserialize<T>(buffer, DefaultObjectSerializerOptions))
            };
        }

        Task<bool> SetObjectAsync<T>(RedisKey key, T value, TimeSpan? expiry = null, When when = When.Always, CancellationToken cancellationToken = default) where T : class
            => SetAsync(
                key,
                value == null
                    ? Array.Empty<byte>()
                    : MessagePackSerializer.Serialize(value, DefaultObjectSerializerOptions),
                expiry,
                when,
                cancellationToken);

        Task<byte[]> GetAsync(RedisKey key, CancellationToken cancellationToken = default);
        Task<bool> SetAsync(RedisKey key, RedisValue value, TimeSpan? expiry = null, When when = When.Always, CancellationToken cancellationToken = default);

        Task<bool> SetIfEqualAsync(RedisKey key, RedisValue value, RedisValue comparand, TimeSpan? expiry = null, When when = When.Always, CancellationToken cancellationToken = default);

        Task<long?> GetIntegerAsync(RedisKey key, CancellationToken cancellationToken = default);
        Task<long?> IncrementAsync(RedisKey key, long delta = 1, TimeSpan? expiry = null, When when = When.Always, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(RedisKey key, CancellationToken cancellationToken = default);
    }

    public interface IRedisSetHandler
    {
        Task<RedisValue[]> GetAsync(CancellationToken cancellationToken = default);
        Task<RedisValue[]> GetRandomAsync(int count = 1, CancellationToken cancellationToken = default);
        Task<RedisValue[]> RemoveRandomAsync(int count = 1, CancellationToken cancellationToken = default);

        Task<long> CountAsync(CancellationToken cancellationToken = default);

        Task<bool> AddAsync(RedisValue value, CancellationToken cancellationToken = default);
        Task<bool> AddAsync(RedisValue[] values, CancellationToken cancellationToken = default);

        Task<bool> RemoveAsync(RedisValue value, CancellationToken cancellationToken = default);
        Task<bool> RemoveAsync(RedisValue[] value, CancellationToken cancellationToken = default);

        Task<bool> ContainsAsync(RedisValue value, CancellationToken cancellationToken = default);
    }

    public interface IRedisSortedSetHandler
    {
        Task<RedisValue[]> GetAsync(CancellationToken cancellationToken = default);
        Task<double?> GetAsync(RedisValue value, CancellationToken cancellationToken = default);
        Task<byte[]> GetAsync(double score, CancellationToken cancellationToken = default);

        Task<long> CountAsync(CancellationToken cancellationToken = default);
        Task<long> CountAsync(double min = double.NegativeInfinity, double max = double.PositiveInfinity, CancellationToken cancellationToken = default);

        Task<RedisValue[]> RangeAsync(double min = double.NegativeInfinity, double max = double.PositiveInfinity, CancellationToken cancellationToken = default);
        Task<SortedSetEntry[]> RangeScoresAsync(double min = double.NegativeInfinity, double max = double.PositiveInfinity, CancellationToken cancellationToken = default);

        Task<bool> AddAsync(RedisValue value, double score, CancellationToken cancellationToken = default);
        Task<bool> AddOrUpdateAsync(RedisValue value, double score, CancellationToken cancellationToken = default);

        Task<bool> RemoveAsync(RedisValue value, CancellationToken cancellationToken = default);
        Task<bool> RemoveAsync(double score, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes this set if no member has score in the given range.
        /// </summary>
        Task<bool> DeleteIfNoneAsync(double min = double.NegativeInfinity, double max = double.PositiveInfinity, CancellationToken cancellationToken = default);

        Task<double> IncrementAsync(RedisValue value, double delta = 1, CancellationToken cancellationToken = default);
        Task<double> DecrementAsync(RedisValue value, double delta = 1, CancellationToken cancellationToken = default) => IncrementAsync(value, -delta, cancellationToken);

        async Task<bool> ContainsAsync(RedisValue value, CancellationToken cancellationToken = default) => await GetAsync(value, cancellationToken) != null;
        async Task<bool> ContainsAsync(double score, CancellationToken cancellationToken = default) => await GetAsync(score, cancellationToken) != null;
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
        readonly IOptionsMonitor<RedisOptions> _options;
        readonly ILogger<RedisClient> _logger; // logger can be null when debug is disabled

        readonly RedisKey _keyPrefix; // key prefix is cached for performance
        readonly RedisKeyMemory _keyMemory;

        public RedisClient(IOptionsMonitor<RedisOptions> options, ILogger<RedisClient> logger, IHostEnvironment environment)
        {
            _options = options;
            _logger  = logger.IsEnabled(LogLevel.Debug) ? logger : null;

            _keyPrefix = _options.CurrentValue.KeyPrefix;
            _keyMemory = environment.IsDevelopment() ? new RedisKeyMemory() : null;
        }

        public ConnectionMultiplexer ConnectionMultiplexer { get; private set; }

        IDatabase _database;

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            var measure = new MeasureContext();
            var options = _options.CurrentValue;

            ConnectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(options.Endpoint);

            _database = ConnectionMultiplexer.GetDatabase();

            _logger?.LogDebug($"Connected to redis in {measure}: {options.Endpoint}");
        }

        public async Task<byte[]> GetAsync(RedisKey key, CancellationToken cancellationToken = default)
        {
            _keyMemory?.Add(key);

            return await _database.StringGetAsync(key.Prepend(_keyPrefix));
        }

        public async Task<bool> SetAsync(RedisKey key, RedisValue value, TimeSpan? expiry = null, When when = When.Always, CancellationToken cancellationToken = default)
        {
            _keyMemory?.Add(key);

            var success = await _database.StringSetAsync(key.Prepend(_keyPrefix), value, expiry, when);

            if (success)
                _logger?.LogDebug($"Set: key={key.ToStringSafe()} expiry={expiry?.ToString() ?? "<none>"}");

            return success;
        }

        public async Task<bool> SetIfEqualAsync(RedisKey key, RedisValue value, RedisValue comparand, TimeSpan? expiry = null, When when = When.Always, CancellationToken cancellationToken = default)
        {
            _keyMemory?.Add(key);

            var transaction = _database.CreateTransaction();

            transaction.AddCondition(Condition.StringEqual(key.Prepend(_keyPrefix), comparand));

            var set = transaction.StringSetAsync(key.Prepend(_keyPrefix), value, expiry, when);

            return await transaction.ExecuteAsync() && await set;
        }

        public async Task<long?> GetIntegerAsync(RedisKey key, CancellationToken cancellationToken = default)
        {
            _keyMemory?.Add(key);

            var value = await _database.StringGetAsync(key.Prepend(_keyPrefix));

            return value.HasValue ? (long) value : null as long?;
        }

        public async Task<long?> IncrementAsync(RedisKey key, long delta = 1, TimeSpan? expiry = null, When when = When.Always, CancellationToken cancellationToken = default)
        {
            _keyMemory?.Add(key);

            var prefixedKey = key.Prepend(_keyPrefix);

            var transaction = _database.CreateTransaction();

            switch (when)
            {
                case When.Always:
                    break;

                case When.Exists:
                    transaction.AddCondition(Condition.KeyExists(prefixedKey));
                    break;

                case When.NotExists:
                    transaction.AddCondition(Condition.KeyNotExists(prefixedKey));
                    break;

                default: throw new ArgumentOutOfRangeException(nameof(when), when, null);
            }

            var increment = transaction.StringIncrementAsync(prefixedKey, delta);
            var expire    = transaction.KeyExpireAsync(prefixedKey, expiry);

            if (await transaction.ExecuteAsync() && await expire)
            {
                var value = await increment;

                _logger?.LogDebug($"Incremented: key={key.ToStringSafe()} delta={delta} expiry={expiry?.ToString() ?? "<none>"}");

                return value;
            }

            return null;
        }

        public async Task<bool> DeleteAsync(RedisKey key, CancellationToken cancellationToken = default)
        {
            _keyMemory?.Remove(key);

            var success = await _database.KeyDeleteAsync(key.Prepend(_keyPrefix));

            if (success)
                _logger?.LogDebug($"Deleted: key={key.ToStringSafe()}");

            return success;
        }

        public IRedisHashHandler Hash(RedisKey key)
        {
            _keyMemory?.Add(key);

            return new HashHandler(_database, key.Prepend(_keyPrefix), _logger);
        }

        public IRedisSetHandler Set(RedisKey key)
        {
            _keyMemory?.Add(key);

            return new SetHandler(_database, key.Prepend(_keyPrefix), _logger);
        }

        public IRedisSortedSetHandler SortedSet(RedisKey key)
        {
            _keyMemory?.Add(key);

            return new SortedSetHandler(_database, key.Prepend(_keyPrefix), _logger);
        }

        sealed class HashHandler : IRedisHashHandler
        {
            readonly IDatabase _database;
            readonly RedisKey _key;
            readonly ILogger<RedisClient> _logger;

            public HashHandler(IDatabase database, RedisKey key, ILogger<RedisClient> logger)
            {
                _database = database;
                _key      = key;
                _logger   = logger;
            }

            public async Task<byte[]> GetAsync(RedisKey key, CancellationToken cancellationToken = default)
                => await _database.HashGetAsync(_key, (RedisValue) (byte[]) key);

            public async Task<bool> SetAsync(RedisKey key, RedisValue value, TimeSpan? expiry = null, When when = When.Always, CancellationToken cancellationToken = default)
            {
                var success = await _database.HashSetAsync(_key, (RedisValue) (byte[]) key, value, when);

                if (success)
                    _logger?.LogDebug($"Set: hash={_key.ToStringSafe()} field={key.ToStringSafe()}");

                return success;
            }

            public async Task<bool> SetIfEqualAsync(RedisKey key, RedisValue value, RedisValue comparand, TimeSpan? expiry = null, When when = When.Always, CancellationToken cancellationToken = default)
            {
                var transaction = _database.CreateTransaction();

                transaction.AddCondition(Condition.HashEqual(_key, (RedisValue) (byte[]) key, comparand));

                var set = transaction.HashSetAsync(_key, (RedisValue) (byte[]) key, value, when);

                return await transaction.ExecuteAsync() && await set;
            }

            public async Task<long?> GetIntegerAsync(RedisKey key, CancellationToken cancellationToken = default)
            {
                var value = await _database.HashGetAsync(_key, (RedisValue) (byte[]) key);

                return value.HasValue ? (long) value : null as long?;
            }

            public async Task<long?> IncrementAsync(RedisKey key, long delta = 1, TimeSpan? expiry = null, When when = When.Always, CancellationToken cancellationToken = default)
            {
                if (when == When.Always)
                    return await _database.HashIncrementAsync(_key, (RedisValue) (byte[]) key, delta);

                var transaction = _database.CreateTransaction();

                switch (when)
                {
                    case When.Always:
                        break;
                    case When.Exists:
                        transaction.AddCondition(Condition.KeyExists(key));
                        break;
                    case When.NotExists:
                        transaction.AddCondition(Condition.KeyNotExists(key));
                        break;

                    default: throw new ArgumentOutOfRangeException(nameof(when), when, null);
                }

                var increment = transaction.HashIncrementAsync(_key, (RedisValue) (byte[]) key, delta);

                if (await transaction.ExecuteAsync())
                {
                    var value = await increment;

                    _logger?.LogDebug($"Incremented: hash={_key.ToStringSafe()} field={key.ToStringSafe()} delta={delta}");

                    return value;
                }

                return null;
            }

            public async Task<bool> DeleteAsync(RedisKey key, CancellationToken cancellationToken = default)
            {
                var success = await _database.HashDeleteAsync(_key, (RedisValue) (byte[]) key);

                if (success)
                    _logger?.LogDebug($"Deleted: hash={_key.ToStringSafe()} field={key.ToStringSafe()}");

                return success;
            }
        }

        sealed class SetHandler : IRedisSetHandler
        {
            readonly IDatabase _database;
            readonly RedisKey _key;
            readonly ILogger<RedisClient> _logger;

            public SetHandler(IDatabase database, RedisKey key, ILogger<RedisClient> logger)
            {
                _database = database;
                _key      = key;
                _logger   = logger;
            }

            public Task<RedisValue[]> GetAsync(CancellationToken cancellationToken = default)
                => _database.SetMembersAsync(_key);

            public Task<RedisValue[]> GetRandomAsync(int count = 1, CancellationToken cancellationToken = default)
                => _database.SetRandomMembersAsync(_key, count);

            public Task<RedisValue[]> RemoveRandomAsync(int count = 1, CancellationToken cancellationToken = default)
                => _database.SetPopAsync(_key, count);

            public Task<long> CountAsync(CancellationToken cancellationToken = default)
                => _database.SetLengthAsync(_key);

            public async Task<bool> AddAsync(RedisValue value, CancellationToken cancellationToken = default)
            {
                var success = await _database.SetAddAsync(_key, value);

                if (success)
                    _logger?.LogDebug($"Added: set={_key.ToStringSafe()}");

                return success;
            }

            public async Task<bool> AddAsync(RedisValue[] values, CancellationToken cancellationToken = default)
            {
                var success = await _database.SetAddAsync(_key, values);

                if (success != 0)
                    _logger?.LogDebug($"Added {values.Length} items: set={_key.ToStringSafe()}");

                return success != 0;
            }

            public async Task<bool> RemoveAsync(RedisValue value, CancellationToken cancellationToken = default)
            {
                var success = await _database.SetRemoveAsync(_key, value);

                if (success)
                    _logger?.LogDebug($"Removed: set={_key.ToStringSafe()}");

                return success;
            }

            public async Task<bool> RemoveAsync(RedisValue[] values, CancellationToken cancellationToken = default)
            {
                var success = await _database.SetRemoveAsync(_key, values);

                if (success != 0)
                    _logger?.LogDebug($"Removed {values.Length} items: set={_key.ToStringSafe()}");

                return success != 0;
            }

            public Task<bool> ContainsAsync(RedisValue value, CancellationToken cancellationToken = default)
                => _database.SetContainsAsync(_key, value);
        }

        sealed class SortedSetHandler : IRedisSortedSetHandler
        {
            readonly IDatabase _database;
            readonly RedisKey _key;
            readonly ILogger<RedisClient> _logger;

            public SortedSetHandler(IDatabase database, RedisKey key, ILogger<RedisClient> logger)
            {
                _database = database;
                _key      = key;
                _logger   = logger;
            }

            public Task<RedisValue[]> GetAsync(CancellationToken cancellationToken = default)
                => _database.SortedSetRangeByRankAsync(_key);

            public Task<double?> GetAsync(RedisValue value, CancellationToken cancellationToken = default)
                => _database.SortedSetScoreAsync(_key, value);

            public async Task<byte[]> GetAsync(double score, CancellationToken cancellationToken = default)
                => (await _database.SortedSetRangeByScoreAsync(_key, score, score)).FirstOrDefault();

            public Task<long> CountAsync(CancellationToken cancellationToken = default)
                => _database.SortedSetLengthAsync(_key);

            public Task<long> CountAsync(double min = double.NegativeInfinity, double max = double.PositiveInfinity, CancellationToken cancellationToken = default)
                => _database.SortedSetLengthAsync(_key, min, max);

            public Task<RedisValue[]> RangeAsync(double min, double max, CancellationToken cancellationToken = default)
                => _database.SortedSetRangeByScoreAsync(_key, min, max);

            public Task<SortedSetEntry[]> RangeScoresAsync(double min = double.NegativeInfinity, double max = double.PositiveInfinity, CancellationToken cancellationToken = default)
                => _database.SortedSetRangeByScoreWithScoresAsync(_key, min, max);

            public async Task<bool> AddAsync(RedisValue value, double score, CancellationToken cancellationToken = default)
            {
                var success = await _database.SortedSetAddAsync(_key, value, score, When.NotExists);

                if (success)
                    _logger?.LogDebug($"Added: sorted set={_key.ToStringSafe()} score={score}");

                return success;
            }

            public async Task<bool> AddOrUpdateAsync(RedisValue value, double score, CancellationToken cancellationToken = default)
            {
                var success = await _database.SortedSetAddAsync(_key, value, score);

                if (success)
                    _logger?.LogDebug($"Upserted: sorted set={_key.ToStringSafe()} score={score}");

                return success;
            }

            public async Task<bool> RemoveAsync(double score, CancellationToken cancellationToken = default)
            {
                var success = await _database.SortedSetRemoveRangeByScoreAsync(_key, score, score);

                if (success == 1)
                    _logger?.LogDebug($"Removed: sorted set={_key.ToStringSafe()} score={score}");

                return success == 1;
            }

            public async Task<bool> RemoveAsync(RedisValue value, CancellationToken cancellationToken = default)
            {
                var success = await _database.SortedSetRemoveAsync(_key, value);

                if (success)
                    _logger?.LogDebug($"Removed: sorted set={_key.ToStringSafe()}");

                return success;
            }

            public async Task<bool> DeleteIfNoneAsync(double min = double.NegativeInfinity, double max = double.PositiveInfinity, CancellationToken cancellationToken = default)
            {
                do
                {
                    if (await _database.SortedSetLengthAsync(_key, min, max) != 0)
                        return false;

                    var transaction = _database.CreateTransaction();

                    transaction.AddCondition(Condition.SortedSetLengthEqual(_key, 0, min, max));

                    var delete = transaction.KeyDeleteAsync(_key);

                    if (await transaction.ExecuteAsync())
                        return await delete;
                }
                while (true);
            }

            public Task<double> IncrementAsync(RedisValue value, double delta = 1, CancellationToken cancellationToken = default)
                => _database.SortedSetIncrementAsync(_key, value, delta);
        }

        public async Task ResetAsync(CancellationToken cancellationToken)
        {
            if (_keyMemory == null)
                return;

            var keys = _keyMemory.Clear(_keyPrefix);

            if (keys.Length == 0)
                return;

            await _database.KeyDeleteAsync(keys);

            _logger?.LogDebug($"Deleted: all known {keys.Length} keys");
        }

        public void Dispose() => ConnectionMultiplexer?.Dispose();
    }
}