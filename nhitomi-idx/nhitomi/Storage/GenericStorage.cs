using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;

namespace nhitomi.Storage
{
    public enum StorageType
    {
        /// <summary>
        /// <see cref="MemoryStorage"/>
        /// </summary>
        Memory,

        /// <summary>
        /// <see cref="LocalStorage"/>
        /// </summary>
        Local,

        /// <summary>
        /// <see cref="CachedStorage"/>
        /// </summary>
        Cached,

        /// <summary>
        /// <see cref="S3Storage"/>
        /// </summary>
        S3
    }

    /// <summary>
    /// Represents a generic polymorphic storage that can be configured to use a specific backend.
    /// You must configure exactly one backend.
    /// </summary>
    public class StorageOptions
    {
        object _inner = new MemoryStorageOptions();

        /// <summary>
        /// Type of the storage backend. This must be set first before setting other options properties.
        /// </summary>
        public StorageType Type
        {
            get => _inner switch
            {
                MemoryStorageOptions _ => StorageType.Memory,
                LocalStorageOptions _  => StorageType.Local,
                CachedStorageOptions _ => StorageType.Cached,
                S3StorageOptions _     => StorageType.S3,

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
                    StorageType.S3     => new S3StorageOptions(),

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

        public S3StorageOptions S3
        {
            get => _inner as S3StorageOptions;
            set => SetInner(value);
        }
    }

    /// <summary>
    /// A storage implementation that delegates calls to another storage implementation configured by <see cref="StorageOptions"/>.
    /// </summary>
    public class GenericStorage : IStorage
    {
        readonly IStorage _impl;

        public GenericStorage(IServiceProvider services, StorageOptions options)
        {
            if (options.Memory != null)
                _impl = ActivatorUtilities.CreateInstance<MemoryStorage>(services, options.Memory);

            else if (options.Local != null)
                _impl = ActivatorUtilities.CreateInstance<LocalStorage>(services, options.Local);

            else if (options.Cached != null)
                _impl = ActivatorUtilities.CreateInstance<CachedStorage>(services, options.Cached);

            else if (options.S3 != null)
                _impl = ActivatorUtilities.CreateInstance<S3Storage>(services, options.S3);

            else throw new NotSupportedException("Unsupported storage.");

            services.GetService<ILogger<GenericStorage>>().LogWarning($"Created storage implementation: {_impl.GetType().Name}");
        }

        public virtual Task InitializeAsync(CancellationToken cancellationToken = default) => _impl.InitializeAsync(cancellationToken);
        public virtual Task<OneOf<StorageFile, NotFound, Exception>> ReadAsync(string name, CancellationToken cancellationToken = default) => _impl.ReadAsync(name, cancellationToken);
        public virtual Task<OneOf<Success, Exception>> WriteAsync(StorageFile file, CancellationToken cancellationToken = default) => _impl.WriteAsync(file, cancellationToken);
        public virtual Task DeleteAsync(string[] names, CancellationToken cancellationToken = default) => _impl.DeleteAsync(names, cancellationToken);

        public void Dispose() => _impl.Dispose();
    }
}