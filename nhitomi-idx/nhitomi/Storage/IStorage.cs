using System;
using System.Threading;
using System.Threading.Tasks;
using OneOf;
using OneOf.Types;

namespace nhitomi.Storage
{
    /// <summary>
    /// An abstract asynchronous storage interface.
    /// </summary>
    public interface IStorage : IDisposable
    {
        Task InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads a file of the given name from this storage.
        /// </summary>
        Task<OneOf<StorageFile, NotFound, Exception>> ReadAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Writes the given file to this storage.
        /// </summary>
        /// <returns>True if the write succeeded; otherwise false.</returns>
        Task<OneOf<Success, Exception>> WriteAsync(StorageFile file, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a single file from this storage.
        /// </summary>
        Task DeleteAsync(string name, CancellationToken cancellationToken = default) => DeleteAsync(new[] { name }, cancellationToken);

        /// <summary>
        /// Deletes multiple files from this storage. This does not guarantee atomicity.
        /// </summary>
        Task DeleteAsync(string[] names, CancellationToken cancellationToken = default);
    }
}