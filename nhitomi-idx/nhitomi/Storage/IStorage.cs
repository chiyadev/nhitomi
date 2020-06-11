using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
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

        async Task<string> ReadStringAsync(string name, CancellationToken cancellationToken = default, Encoding encoding = null)
        {
            var data = null as string;

            var result = await ReadAsync(name, cancellationToken);

            if (result.TryPickT0(out var file, out _))
                await using (file)
                    data = (encoding ?? Encoding.UTF8).GetString(await file.Stream.ToArrayAsync(cancellationToken));

            return string.IsNullOrEmpty(data) ? null : data;
        }

        async Task WriteStringAsync(string name, string data, CancellationToken cancellationToken = default, string mediaType = "text/plain", Encoding encoding = null)
        {
            data ??= "";

            await using var file = new StorageFile
            {
                Name      = name,
                MediaType = mediaType,
                Stream    = new MemoryStream((encoding ?? Encoding.UTF8).GetBytes(data))
            };

            await WriteAsync(file, cancellationToken);
        }

        async Task<T> ReadObjectAsync<T>(string name, CancellationToken cancellationToken = default, Encoding encoding = null)
        {
            var data = await ReadStringAsync(name, cancellationToken, encoding);

            if (data == null)
                return default;

            return JsonConvert.DeserializeObject<T>(data);
        }

        Task WriteObjectAsync<T>(string name, T value, CancellationToken cancellationToken = default, Encoding encoding = null)
        {
            var data = JsonConvert.SerializeObject(value);

            return WriteStringAsync(name, data, cancellationToken, "application/json", encoding);
        }
    }
}