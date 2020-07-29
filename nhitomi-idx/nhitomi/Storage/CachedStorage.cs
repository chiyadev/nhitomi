using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using nhitomi.Database;
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
        /// Expiry of cached files.
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
        readonly CachedStorageOptions _options;
        readonly IRedisClient _redis;
        readonly ILogger<CachedStorage> _logger;
        readonly RecyclableMemoryStreamManager _memory;

        public CachedStorage(IServiceProvider services, CachedStorageOptions options, IRedisClient redis, ILogger<CachedStorage> logger, RecyclableMemoryStreamManager memory)
        {
            _impl    = new DefaultStorage(services, options.Inner);
            _options = options;
            _redis   = redis;
            _logger  = logger;
            _memory  = memory;
        }

        public Task InitializeAsync(CancellationToken cancellationToken = default) => _impl.InitializeAsync(cancellationToken);

        public async Task<OneOf<StorageFile, NotFound, Exception>> ReadAsync(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                var cache = await _redis.GetObjectAsync<Cache>(_options.Prefix + name, cancellationToken);

                if (cache == null)
                {
                    var result = await _impl.ReadAsync(name, cancellationToken);

                    if (result.TryPickT0(out var file, out _))
                        await using (file)
                        {
                            cache = new Cache
                            {
                                Data      = await file.Stream.ToArrayAsync(cancellationToken),
                                MediaType = file.MediaType
                            };

                            await _redis.SetObjectAsync(_options.Prefix + name, cache, _options.Expiry, cancellationToken: cancellationToken);
                        }
                }

                if (cache == null)
                    return new NotFound();

                return new StorageFile
                {
                    Name      = name,
                    MediaType = cache.MediaType,
                    Stream    = _memory.GetStream(cache.Data)
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
            byte[] data;

            if (file.Stream is MemoryStream memory)
            {
                data = memory.ToArray();
            }
            else
            {
                data = await file.Stream.ToArrayAsync(cancellationToken);

                file.Stream.Dispose();
                file.Stream = _memory.GetStream(data);
            }

            var result = await _impl.WriteAsync(file, cancellationToken);

            if (result.IsT0)
            {
                var cache = new Cache
                {
                    Data      = data,
                    MediaType = file.MediaType
                };

                await _redis.SetObjectAsync(_options.Prefix + file.Name, cache, _options.Expiry, cancellationToken: CancellationToken.None); // don't cancel
            }

            return result;
        }

        public async Task DeleteAsync(string[] names, CancellationToken cancellationToken = default)
        {
            await _impl.DeleteAsync(names, cancellationToken);

            await Task.WhenAll(names.Select(n => _redis.DeleteAsync(_options.Prefix + n, CancellationToken.None))); // don't cancel
        }

        public void Dispose() => _impl.Dispose();
    }
}