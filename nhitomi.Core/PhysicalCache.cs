// Copyright (c) 2019 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace nhitomi.Core
{
    public class PhysicalCache
    {
        public string CachePath { get; set; }
        public JsonSerializer Serializer { get; set; }

        public PhysicalCache(
            string name,
            JsonSerializer serializer = null
        )
        {
            CachePath = Path.GetTempPath();
            CachePath = getPath(nameof(nhitomi));
            CachePath = getPath(name);

            Serializer = serializer ?? JsonSerializer.CreateDefault();
        }

        public async Task<Stream> GetStreamAsync(string name)
        {
            while (true)
                try
                {
                    return new FileStream(getPath(name), FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                catch (FileNotFoundException) { throw; }
                catch (DirectoryNotFoundException) { throw; }
                catch (IOException)
                {
                    // Cache is still being written. Sleep.
                    await Task.Delay(200);
                }
        }

        public async Task CreateStreamAsync(string name, Func<Task<Stream>> getAsync)
        {
            try
            {
                var path = getPath(name);

                Directory.CreateDirectory(Path.GetDirectoryName(path));

                // Create new cache if possible
                // This will fail if cache already exists
                using (var cacheStream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    var stream = await getAsync();

                    // Write to cache
                    await stream.CopyToAsync(cacheStream);
                }
            }
            catch (IOException)
            {
                // Cache already exists
            }
        }

        public async Task<Stream> GetOrCreateStreamAsync(string name, Func<Task<Stream>> getAsync)
        {
            await CreateStreamAsync(name, getAsync);

            return await GetStreamAsync(name);
        }

        public async Task<T> GetAsync<T>(string name)
        {
            using (var stream = await GetStreamAsync(name))
            {
                using (var reader = new StreamReader(stream))
                using (var jsonReader = new JsonTextReader(reader))
                    return Serializer.Deserialize<T>(jsonReader);
            }
        }

        public Task CreateAsync<T>(string name, Func<Task<T>> getAsync)
        {
            return CreateStreamAsync(name, async () =>
            {
                using (var writer = new StringWriter())
                using (var jsonWriter = new JsonTextWriter(writer))
                {
                    Serializer.Serialize(jsonWriter, await getAsync());

                    return new MemoryStream(Encoding.Default.GetBytes(writer.ToString()));
                }
            });
        }

        public async Task<T> GetOrCreateAsync<T>(string name, Func<Task<T>> getAsync)
        {
            await CreateAsync(name, getAsync);

            return await GetAsync<T>(name);
        }

        string getPath(string name) => Path.Combine(CachePath, processName(name));
        static string processName(string name) => name.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    }
}