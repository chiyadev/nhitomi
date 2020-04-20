using System;
using System.Threading;
using System.Threading.Tasks;

namespace nhitomi
{
    public interface ICacheStore
    {
        /// <summary>
        /// Retrieves a cached object by the specified key, and updates the cache using the provided callback if unavailable.
        /// </summary>
        Task<T> GetAsync<T>(string key, Func<Task<T>> get = null, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Updates a cached object by the specified key and returns the new value.
        /// </summary>
        Task<T> SetAsync<T>(string key, T value, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Deletes a cached object by the specified key and returns the cached value.
        /// Internally, this will set the cache to null instead of deleting the entry to prevent cache penetration.
        /// </summary>
        Task<T> DeleteAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
            => SetAsync<T>(key, null, cancellationToken);
    }

    public interface ICacheStore<T> : ICacheStore where T : class
    {
        /// <inheritdoc cref="ICacheStore.GetAsync{T}"/>
        Task<T> GetAsync(string key, Func<Task<T>> get = null, CancellationToken cancellationToken = default) => GetAsync<T>(key, get, cancellationToken);

        /// <inheritdoc cref="ICacheStore.SetAsync{T}"/>
        Task<T> SetAsync(string key, T value, CancellationToken cancellationToken = default) => SetAsync<T>(key, value, cancellationToken);

        /// <inheritdoc cref="ICacheStore.DeleteAsync{T}"/>
        Task<T> DeleteAsync(string key, CancellationToken cancellationToken = default) => DeleteAsync<T>(key, cancellationToken);
    }

    public interface ICacheCounter
    {
        Task<long> GetAsync(Func<Task<long>> get = null, CancellationToken cancellationToken = default);
        Task<long> SetAsync(long value, CancellationToken cancellationToken = default);
        Task<long> IncrementAsync(long delta = 1, CancellationToken cancellationToken = default);
        Task<long> DecrementAsync(long delta = 1, CancellationToken cancellationToken = default) => IncrementAsync(-delta, cancellationToken);

        Task ResetAsync(CancellationToken cancellationToken = default);
    }

    public delegate string CacheKeyTransform(string key);

    public interface ICacheManager
    {
        public static readonly CacheKeyTransform DefaultKeyTransform = s => s;

        ICacheStore CreateStore(CacheKeyTransform transform = null, TimeSpan? expiry = null);
        ICacheStore<T> CreateStore<T>(CacheKeyTransform transform = null, TimeSpan? expiry = null) where T : class;

        ICacheCounter CreateCounter(string key, TimeSpan? expiry = null);
    }
}