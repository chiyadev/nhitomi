using System;
using System.Threading;
using System.Threading.Tasks;

namespace nhitomi
{
    public delegate Task CacheSetAsyncCallback<in T>(T value, CancellationToken cancellationToken = default) where T : class;

    public delegate Task CacheDeleteAsyncCallback<in T>(T value, CancellationToken cancellationToken = default) where T : class;

    public interface ICacheStore : ICacheStoreEvents
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

        /// <summary>
        /// Binds to another store such that any changes to that store will be propagated to this store.
        /// <paramref name="keySelector"/> and <paramref name="valueSelector"/> can be used to transform the incoming value.
        /// </summary>
        void SetSource<T, TOther>(ICacheStoreEvents cache, Func<TOther, string> keySelector, Func<TOther, T> valueSelector) where T : class where TOther : class
        {
            cache.OnSet<TOther>((x, c) => SetAsync(keySelector(x), valueSelector(x), c));
            cache.OnDelete<TOther>((x, c) => DeleteAsync<T>(keySelector(x), c));
        }
    }

    public interface ICacheStoreEvents
    {
        /// <summary>
        /// Registers an asynchronous delegate to be invoked when <see cref="ICacheStore.SetAsync{T}(string,T,System.Threading.CancellationToken)"/> succeeds.
        /// </summary>
        void OnSet<T>(CacheSetAsyncCallback<T> callback) where T : class;

        /// <summary>
        /// Registers an asynchronous delegate to be invoked when <see cref="ICacheStore.DeleteAsync{T}(string,System.Threading.CancellationToken)"/> succeeds.
        /// </summary>
        void OnDelete<T>(CacheDeleteAsyncCallback<T> callback) where T : class;
    }

    public interface ICacheStore<T> : ICacheStore where T : class
    {
        /// <inheritdoc cref="ICacheStore.GetAsync{T}"/>
        Task<T> GetAsync(string key, Func<Task<T>> get = null, CancellationToken cancellationToken = default) => GetAsync<T>(key, get, cancellationToken);

        /// <inheritdoc cref="ICacheStore.SetAsync{T}"/>
        Task<T> SetAsync(string key, T value, CancellationToken cancellationToken = default) => SetAsync<T>(key, value, cancellationToken);

        /// <inheritdoc cref="ICacheStore.DeleteAsync{T}"/>
        Task<T> DeleteAsync(string key, CancellationToken cancellationToken = default) => DeleteAsync<T>(key, cancellationToken);

        /// <inheritdoc cref="ICacheStoreEvents.OnSet{T}"/>
        void OnSet(CacheSetAsyncCallback<T> callback) => OnSet<T>(callback);

        /// <inheritdoc cref="ICacheStoreEvents.OnDelete{T}"/>
        void OnDelete(CacheDeleteAsyncCallback<T> callback) => OnDelete<T>(callback);

        /// <inheritdoc cref="ICacheStore.SetSource{T,TOther}"/>
        void SetSource<TOther>(ICacheStoreEvents cache, Func<TOther, string> keySelector, Func<TOther, T> valueSelector) where TOther : class => SetSource<T, TOther>(cache, keySelector, valueSelector);
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