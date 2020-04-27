using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;

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
        readonly ILogger<CachedStorage> _logger;

        public CachedStorage(IServiceProvider services, ICacheManager cache, CachedStorageOptions options, ILogger<CachedStorage> logger)
        {
            _logger = logger;
            _impl   = new DefaultStorage(services, options.Inner);
            _cache  = cache.CreateStore<Cache>(k => options.Prefix + k, options.Expiry);
        }

        public Task InitializeAsync(CancellationToken cancellationToken = default) => _impl.InitializeAsync(cancellationToken);

        public async Task<OneOf<StorageFile, NotFound, Exception>> ReadAsync(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                var cache = await _cache.GetAsync(name, async () =>
                {
                    var result = await _impl.ReadAsync(name, cancellationToken);

                    if (!result.IsT0)
                        return null;

                    using var file = result.AsT0;

                    return new Cache
                    {
                        Data      = await file.Stream.ToArrayAsync(cancellationToken),
                        MediaType = file.MediaType
                    };
                }, cancellationToken);

                if (cache == null)
                    return new NotFound();

                return new StorageFile
                {
                    Name      = name,
                    MediaType = cache.MediaType,
                    Stream    = new MemoryStream(cache.Data)
                };
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Failed to read file '{name}'.");
                return e;
            }
        }

        public async Task<OneOf<Success, Exception>> WriteAsync(StorageFile file, CancellationToken cancellationToken = default)
        {
            var data = await file.Stream.ToArrayAsync(cancellationToken);

            file.Stream.Dispose();
            file.Stream = new MemoryStream(data);

            var result = await _impl.WriteAsync(file, cancellationToken);

            if (!result.IsT0)
                return result;

            cancellationToken = default; // don't cancel cache write

            await _cache.SetAsync(file.Name, new Cache
            {
                Data      = data,
                MediaType = file.MediaType
            }, cancellationToken);

            return new Success();
        }

        public async Task DeleteAsync(string[] names, CancellationToken cancellationToken = default)
        {
            await _impl.DeleteAsync(names, cancellationToken);

            cancellationToken = default; // don't cancel cache delete

            await Task.WhenAll(names.Select(n => _cache.DeleteAsync(n, cancellationToken)));
        }

        public void Dispose() => _impl.Dispose();
    }
}