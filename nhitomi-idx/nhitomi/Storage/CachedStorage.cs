using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;

namespace nhitomi.Storage
{
    public class CachedStorageOptions
    {
        /// <summary>
        /// Prefix to use for cache keys.
        /// </summary>
        public string Prefix { get; set; } = "fs:";

        /// <summary>
        /// Expiry of caches.
        /// </summary>
        public TimeSpan Expiry { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Inner storage.
        /// </summary>
        public StorageOptions Inner { get; set; } = new StorageOptions();
    }

    public class CachedStorage : IStorage
    {
        [MessagePackObject]
        public sealed class Cache
        {
            [Key(0)]
            public byte[] Data { get; set; }

            [Key(1)]
            public string MediaType { get; set; }
        }

        readonly DefaultStorage _impl;
        readonly ICacheStore<Cache> _cache;

        public CachedStorage(IServiceProvider services, ICacheManager cache, CachedStorageOptions options)
        {
            _impl  = new DefaultStorage(services, options.Inner);
            _cache = cache.CreateStore<Cache>(k => options.Prefix + k, options.Expiry);
        }

        public Task InitializeAsync(CancellationToken cancellationToken = default) => _impl.InitializeAsync(cancellationToken);

        public async Task<StorageFile> ReadAsync(string name, CancellationToken cancellationToken = default)
        {
            var cache = await _cache.GetAsync(name, async () =>
            {
                var file = await _impl.ReadAsync(name, cancellationToken);

                if (file == null)
                    return null;

                return new Cache
                {
                    Data      = await file.Stream.ToArrayAsync(cancellationToken),
                    MediaType = file.MediaType
                };
            }, cancellationToken);

            if (cache == null)
                return null;

            return new StorageFile
            {
                Name      = name,
                MediaType = cache.MediaType,
                Stream    = new MemoryStream(cache.Data)
            };
        }

        public async Task<bool> WriteAsync(StorageFile file, CancellationToken cancellationToken = default)
        {
            var data = await file.Stream.ToArrayAsync(cancellationToken);

            file.Stream.Dispose();
            file.Stream = new MemoryStream(data);

            if (!await _impl.WriteAsync(file, cancellationToken))
                return false;

            cancellationToken = default; // don't cancel to write cache

            await _cache.SetAsync(file.Name, new Cache
            {
                Data      = data,
                MediaType = file.MediaType
            }, cancellationToken);

            return true;
        }

        public async Task DeleteAsync(string[] names, CancellationToken cancellationToken = default)
        {
            await _impl.DeleteAsync(names, cancellationToken);

            cancellationToken = default; // don't cancel to write cache

            await Task.WhenAll((IEnumerable<Task>) names.Select(n => _cache.DeleteAsync(n, cancellationToken)));
        }

        public void Dispose() => _impl.Dispose();
    }
}