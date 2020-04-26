using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;

namespace nhitomi.Storage
{
    public class LocalStorageOptions
    {
        public string Path { get; set; } = "data_storage";
    }

    public class LocalStorage : IStorage
    {
        static readonly MessagePackSerializerOptions _serializerOptions =
            MessagePackSerializerOptions
               .Standard
               .WithCompression(MessagePackCompression.Lz4Block);

        [MessagePackObject]
        public sealed class FileContainer
        {
            [Key(0)]
            public byte[] Data { get; set; }

            [Key(1)]
            public string MediaType { get; set; }
        }

        readonly IResourceLocker _locker;
        readonly ILogger<LocalStorage> _logger;

        readonly string _basePath;

        public LocalStorage(IHostEnvironment environment, LocalStorageOptions options, IResourceLocker locker, ILogger<LocalStorage> logger)
        {
            _locker = locker;
            _logger = logger;

            _basePath = Path.Combine(environment.ContentRootPath, options.Path);
        }

        Task IStorage.InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public async Task<OneOf<StorageFile, NotFound, Exception>> ReadAsync(string name, CancellationToken cancellationToken = default)
        {
            var path = GetPath(name);

            try
            {
                byte[] data;

                await using (await _locker.EnterAsync($"storage:{path}", cancellationToken))
                {
                    await using var stream = FileEx.OpenAsync(path, FileMode.Open, FileAccess.Read);

                    data = await stream.ToArrayAsync(cancellationToken);
                }

                var container = MessagePackSerializer.Deserialize<FileContainer>(data, _serializerOptions);

                return new StorageFile
                {
                    Name      = name,
                    Stream    = new MemoryStream(container.Data),
                    MediaType = container.MediaType
                };
            }
            catch (IOException)
            {
                return new NotFound();
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Failed to read file '{name}'.");
                return e;
            }
        }

        public async Task<OneOf<Success, Exception>> WriteAsync(StorageFile file, CancellationToken cancellationToken = default)
        {
            var path = GetPath(file.Name);

            var data = MessagePackSerializer.Serialize(new FileContainer
            {
                Data      = await file.Stream.ToArrayAsync(cancellationToken),
                MediaType = file.MediaType
            }, _serializerOptions);

            try
            {
                using (var temp = new TemporaryFile())
                {
                    // write to temporary file first
                    await using (var stream = temp.OpenAsync())
                    {
                        await stream.WriteAsync(data, cancellationToken);
                        await stream.FlushAsync(cancellationToken);
                    }

                    // move temp file to storage
                    await using (await _locker.EnterAsync($"storage:{path}", cancellationToken))
                        File.Move(temp.Path, path, true);
                }

                _logger.LogInformation($"Wrote file '{file.Name}'.");

                return new Success();
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Failed to write file '{file.Name}'.");
                return e;
            }
        }

        public async Task DeleteAsync(string[] names, CancellationToken cancellationToken = default)
        {
            foreach (var name in names)
            {
                try
                {
                    var path = GetPath(name);

                    await using (await _locker.EnterAsync($"storage:{path}", cancellationToken))
                        File.Delete(path);

                    _logger.LogInformation($"Deleted file '{name}'.");
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, $"Failed to delete file '{name}'.");
                }
            }
        }

        string GetPath(string name)
        {
            var path = Path.Combine(_basePath, name.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            return path;
        }

        public void Dispose() { }
    }
}