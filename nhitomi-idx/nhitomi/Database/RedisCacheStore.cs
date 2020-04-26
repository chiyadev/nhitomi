using System;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace nhitomi.Database
{
    public class RedisCacheStore : ICacheStore
    {
        readonly IRedisClient _client;
        readonly CacheKeyTransform _transform;
        readonly TimeSpan? _expiry;

        public RedisCacheStore(IRedisClient client, CacheKeyTransform transform, TimeSpan? expiry)
        {
            _client    = client;
            _transform = transform;
            _expiry    = expiry;
        }

        public async Task<T> GetAsync<T>(string key, Func<Task<T>> get = null, CancellationToken cancellationToken = default) where T : class
        {
            var (success, value) = await _client.GetObjectAsync<T>(_transform(key), cancellationToken);

            if (success)
                return value;

            if (get == null)
                return default;

            return await SetAsync(key, await get(), cancellationToken);
        }

        public async Task<T> SetAsync<T>(string key, T value, CancellationToken cancellationToken = default) where T : class
        {
            await _client.SetObjectAsync(_transform(key), value, _expiry, default, cancellationToken);

            return value;
        }
    }

    public class RedisCacheStore<T> : RedisCacheStore, ICacheStore<T> where T : class
    {
        public RedisCacheStore(IRedisClient client, CacheKeyTransform transform, TimeSpan? expiry)
            : base(client, transform, expiry) { }
    }

    public class RedisCacheCounter : ICacheCounter
    {
        readonly IRedisClient _client;
        readonly RedisKey _key;
        readonly TimeSpan? _expiry;

        public RedisCacheCounter(IRedisClient client, RedisKey key, TimeSpan? expiry)
        {
            _client = client;
            _key    = key;
            _expiry = expiry;
        }

        public async Task<long> GetAsync(Func<Task<long>> get = null, CancellationToken cancellationToken = default)
        {
            var value = await _client.GetIntegerAsync(_key, cancellationToken);

            if (value != null)
                return value.Value;

            if (get == null)
                return 0;

            return await SetAsync(await get(), cancellationToken);
        }

        public async Task<long> SetAsync(long value, CancellationToken cancellationToken = default)
        {
            await _client.SetAsync(_key, value, _expiry, default, cancellationToken);
            return value;
        }

        public async Task<long> IncrementAsync(long delta = 1, CancellationToken cancellationToken = default)
            => await _client.IncrementAsync(_key, delta, _expiry, default, cancellationToken) ?? 0;

        public Task ResetAsync(CancellationToken cancellationToken = default)
            => _client.DeleteAsync(_key, cancellationToken);
    }

    public class RedisCacheManager : ICacheManager
    {
        readonly IRedisClient _client;

        public RedisCacheManager(IRedisClient client)
        {
            _client = client;
        }

        public ICacheStore CreateStore(CacheKeyTransform transform = null, TimeSpan? expiry = null)
            => new RedisCacheStore(_client, transform ?? ICacheManager.DefaultKeyTransform, expiry);

        public ICacheStore<T> CreateStore<T>(CacheKeyTransform transform = null, TimeSpan? expiry = null) where T : class
            => new RedisCacheStore<T>(_client, transform ?? ICacheManager.DefaultKeyTransform, expiry);

        public ICacheCounter CreateCounter(string key, TimeSpan? expiry = null)
            => new RedisCacheCounter(_client, key, expiry);
    }
}