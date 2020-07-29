using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IO;
using OneOf;
using OneOf.Types;

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

        readonly RecyclableMemoryStreamManager _memory;

        public MemoryStorage(MemoryStorageOptions _, RecyclableMemoryStreamManager memory)
        {
            _memory = memory;
        }

        Task IStorage.InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public async Task<OneOf<StorageFile, NotFound, Exception>> ReadAsync(string name, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            if (!_files.TryGetValue(name, out var file))
                return new NotFound();

            return new StorageFile
            {
                Name      = name,
                Stream    = _memory.GetStream(file.Buffer),
                MediaType = file.MediaType
            };
        }

        public async Task<OneOf<Success, Exception>> WriteAsync(StorageFile file, CancellationToken cancellationToken = default)
        {
            _files[file.Name] = new File
            {
                Buffer    = await file.Stream.ToArrayAsync(cancellationToken),
                MediaType = file.MediaType
            };

            return new Success();
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