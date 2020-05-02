using System;
using System.IO;
using System.Linq;
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
        public string Path { get; set; } = "data";
    }

    public class LocalStorage : IStorage
    {
        static readonly MessagePackSerializerOptions _serializerOptions =
            MessagePackSerializerOptions
               .Standard
               .WithCompression(MessagePackCompression.Lz4BlockArray);

        [MessagePackObject]
        public sealed class FileContainer
        {
            [Key(0)]
            public byte[] Data { get; set; }

            [Key(1)]
            public string MediaType { get; set; }
        }

        readonly ILogger<LocalStorage> _logger;
        readonly string _basePath;

        public LocalStorage(IHostEnvironment environment, LocalStorageOptions options, IResourceLocker locker, ILogger<LocalStorage> logger)
        {
            _logger   = logger;
            _basePath = Path.Combine(environment.ContentRootPath, options.Path);
        }

        Task IStorage.InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        string GetPath(string name)
        {
            var path = Path.Combine(_basePath, name.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            return path;
        }

        const int _fileRetryCount = 10;

        static async Task<FileStream> OpenFileAsync(string path, FileMode mode, FileAccess access, FileShare share, CancellationToken cancellationToken = default)
        {
            var retry = 0;

            do
            {
                try
                {
                    if (!File.Exists(path))
                        throw new FileNotFoundException($"'{path}' not found.");

                    return FileEx.OpenAsync(path, mode, access, share);
                }
                catch (IOException)
                {
                    if (++retry >= _fileRetryCount)
                        throw;

                    await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
                }
            }
            while (true);
        }

        public async Task<OneOf<StorageFile, NotFound, Exception>> ReadAsync(string name, CancellationToken cancellationToken = default)
        {
            var path = GetPath(name);

            try
            {
                byte[] data;

                // concurrent read possible
                await using (var stream = await OpenFileAsync(path, FileMode.Open, FileAccess.Read, FileShare.Read, cancellationToken))
                    data = await stream.ToArrayAsync(cancellationToken);

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

        static async Task MoveFileAsync(string src, string dst, CancellationToken cancellationToken = default)
        {
            var retry = 0;

            do
            {
                try
                {
                    File.Move(src, dst, true);
                    return;
                }
                catch (IOException)
                {
                    if (++retry >= _fileRetryCount)
                        throw;

                    await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
                }
            }
            while (true);
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
                    await MoveFileAsync(temp.Path, path, cancellationToken);
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

        static async Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
        {
            var retry = 0;

            do
            {
                try
                {
                    File.Delete(path);
                    return;
                }
                catch (IOException)
                {
                    if (++retry >= _fileRetryCount)
                        throw;

                    await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
                }
            }
            while (true);
        }

        public Task DeleteAsync(string[] names, CancellationToken cancellationToken = default) => Task.WhenAll(names.Select(async name =>
        {
            try
            {
                await DeleteFileAsync(GetPath(name), cancellationToken);

                _logger.LogInformation($"Deleted file '{name}'.");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Failed to delete file '{name}'.");
            }
        }));

        public void Dispose() { }
    }
}