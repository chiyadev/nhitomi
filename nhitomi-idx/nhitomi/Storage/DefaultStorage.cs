using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace nhitomi.Storage
{
    public enum StorageType
    {
        Memory,
        Local,
        Cached
    }

    /// <summary>
    /// Represents a generic polymorphic storage that can be configured to use a specific backend.
    /// You must configure exactly one backend.
    /// </summary>
    public class StorageOptions
    {
        object _inner = new MemoryStorageOptions();

        /// <summary>
        /// Type of the storage backend.
        /// </summary>
        public StorageType Type
        {
            get => _inner switch
            {
                MemoryStorageOptions _ => StorageType.Memory,
                LocalStorageOptions _  => StorageType.Local,
                CachedStorageOptions _ => StorageType.Cached,

                _ => throw new NotSupportedException($"Unsupported inner storage {_inner?.GetType().Name ?? "<null>"}.")
            };
            set
            {
                if (Type == value)
                    return;

                _inner = value switch
                {
                    StorageType.Memory => new MemoryStorageOptions(),
                    StorageType.Local  => new LocalStorageOptions(),
                    StorageType.Cached => new CachedStorageOptions(),

                    _ => throw new NotSupportedException($"Unsupported inner storage {value}.")
                };
            }
        }

        void SetInner(object o)
        {
            if (_inner?.GetType() == o?.GetType())
                _inner = o;
        }

        /// <summary>
        /// <see cref="MemoryStorage"/>
        /// </summary>
        public MemoryStorageOptions Memory
        {
            get => _inner as MemoryStorageOptions;
            set => SetInner(value);
        }

        /// <summary>
        /// <see cref="LocalStorage"/>
        /// </summary>
        public LocalStorageOptions Local
        {
            get => _inner as LocalStorageOptions;
            set => SetInner(value);
        }

        /// <summary>
        /// <see cref="CachedStorage"/>
        /// </summary>
        public CachedStorageOptions Cached
        {
            get => _inner as CachedStorageOptions;
            set => SetInner(value);
        }
    }

    /// <summary>
    /// A storage implementation that delegates calls to another storage implementation configured by <see cref="StorageOptions"/>.
    /// </summary>
    public class DefaultStorage : IStorage
    {
        readonly IStorage _impl;

        public DefaultStorage(IServiceProvider services, IOptionsMonitor<StorageOptions> options) : this(services, options.CurrentValue) { }

        public DefaultStorage(IServiceProvider services, StorageOptions options)
        {
            if (options.Memory != null)
                _impl = ActivatorUtilities.CreateInstance<MemoryStorage>(services, options.Memory);

            else if (options.Local != null)
                _impl = ActivatorUtilities.CreateInstance<LocalStorage>(services, options.Local);

            else if (options.Cached != null)
                _impl = ActivatorUtilities.CreateInstance<CachedStorage>(services, options.Cached);

            else throw new NotSupportedException("Unsupported storage.");

            services.GetService<ILogger<DefaultStorage>>().LogInformation($"Created storage implementation: {_impl.GetType().Name}");
        }

        public Task InitializeAsync(CancellationToken cancellationToken = default) => _impl.InitializeAsync(cancellationToken);
        public Task<StorageFile> ReadAsync(string name, CancellationToken cancellationToken = default) => _impl.ReadAsync(name, cancellationToken);
        public Task<bool> WriteAsync(StorageFile file, CancellationToken cancellationToken = default) => _impl.WriteAsync(file, cancellationToken);
        public Task DeleteAsync(string[] names, CancellationToken cancellationToken = default) => _impl.DeleteAsync(names, cancellationToken);

        public void Dispose() => _impl.Dispose();
    }
}