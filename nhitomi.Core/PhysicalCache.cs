// Copyright (c) 2018-2019 chiya.dev
//
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace nhitomi.Core
{
    public class PhysicalCache
    {
        public string CachePath { get; set; }
        public JsonSerializer Serializer { get; set; }

        public PhysicalCache(string name, JsonSerializer serializer = null)
        {
            CachePath = Path.GetTempPath();
            CachePath = GetPath(nameof(nhitomi));
            CachePath = GetPath(name);

            Serializer = serializer ?? JsonSerializer.CreateDefault();
        }

        public async Task<Stream> GetStreamAsync(
            string name,
            CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
                try
                {
                    return new FileStream(GetPath(name), FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                catch (FileNotFoundException)
                {
                    throw;
                }
                catch (DirectoryNotFoundException)
                {
                    throw;
                }
                catch (IOException)
                {
                    // Cache is still being written. Sleep.
                    await Task.Delay(100, cancellationToken);
                }

            return null;
        }

        public async Task CreateStreamAsync(
            string name,
            Func<CancellationToken, Task<Stream>> getAsync,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var path = GetPath(name);

                Directory.CreateDirectory(Path.GetDirectoryName(path));

                // Create new cache if possible
                // This will fail if cache already exists
                using (var cacheStream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var getStream = await getAsync(cancellationToken))
                {
                    // Write to cache
                    await getStream.CopyToAsync(cacheStream,4096, cancellationToken);
                }
            }
            catch (IOException)
            {
                // Cache already exists
            }
        }

        public async Task<Stream> GetOrCreateStreamAsync(
            string name,
            Func<CancellationToken, Task<Stream>> getAsync)
        {
            await CreateStreamAsync(name, getAsync);

            return await GetStreamAsync(name);
        }

        public async Task<T> GetAsync<T>(
            string name,
            CancellationToken cancellationToken = default)
        {
            using (var stream = await GetStreamAsync(name, cancellationToken))
            using (var reader = new StreamReader(stream))
            using (var jsonReader = new JsonTextReader(reader))
                return Serializer.Deserialize<T>(jsonReader);
        }

        public Task CreateAsync<T>(
            string name,
            Func<CancellationToken, Task<T>> getAsync,
            CancellationToken cancellationToken = default)
        {
            return CreateStreamAsync(name, async token =>
            {
                using (var writer = new StringWriter())
                using (var jsonWriter = new JsonTextWriter(writer))
                {
                    Serializer.Serialize(jsonWriter, await getAsync(token));

                    return new MemoryStream(Encoding.Default.GetBytes(writer.ToString()));
                }
            }, cancellationToken);
        }

        public async Task<T> GetOrCreateAsync<T>(
            string name,
            Func<CancellationToken, Task<T>> getAsync,
            CancellationToken cancellationToken = default)
        {
            await CreateAsync(name, getAsync, cancellationToken);

            return await GetAsync<T>(name, cancellationToken);
        }

        string GetPath(string name) => Path.Combine(CachePath, ProcessName(name));

        static string ProcessName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            return name.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }
    }
}
