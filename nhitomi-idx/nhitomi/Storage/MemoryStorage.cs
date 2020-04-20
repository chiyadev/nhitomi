using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace nhitomi.Storage
{
    public class MemoryStorageOptions { }

    public class MemoryStorage : IStorage
    {
        readonly ConcurrentDictionary<string, File> _files = new ConcurrentDictionary<string, File>();

        struct File
        {
            public byte[] Buffer;
            public string MediaType;
        }

        public MemoryStorage(MemoryStorageOptions _) { }

        Task IStorage.InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<StorageFile> ReadAsync(string name, CancellationToken cancellationToken = default)
        {
            if (!_files.TryGetValue(name, out var file))
                return Task.FromResult<StorageFile>(null);

            return Task.FromResult(new StorageFile
            {
                Name      = name,
                Stream    = new MemoryStream(file.Buffer),
                MediaType = file.MediaType
            });
        }

        public async Task<bool> WriteAsync(StorageFile file, CancellationToken cancellationToken = default)
        {
            _files[file.Name] = new File
            {
                Buffer    = await file.Stream.ToArrayAsync(cancellationToken),
                MediaType = file.MediaType
            };

            return true;
        }

        public Task DeleteAsync(string[] names, CancellationToken cancellationToken = default)
        {
            foreach (var name in names)
                _files.TryRemove(name, out _);

            return Task.CompletedTask;
        }

        public void Dispose() => _files.Clear();
    }
}