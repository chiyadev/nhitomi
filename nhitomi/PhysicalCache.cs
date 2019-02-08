// Copyright (c) 2019 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.IO;
using System.Threading.Tasks;

namespace nhitomi
{
    public class PhysicalCache
    {
        public string CachePath { get; set; }

        public PhysicalCache(string name)
        {
            CachePath = Path.GetTempPath();
            CachePath = getPath(nameof(nhitomi));
            CachePath = getPath(name);
        }

        public async Task<Stream> GetOrAddAsync(string name, Func<Task<Stream>> getFunc)
        {
            try
            {
                var path = getPath(name);

                Directory.CreateDirectory(Path.GetDirectoryName(path));

                // Create new cache if possible
                // This will fail if cache already exists
                using (var cacheStream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    var stream = await getFunc();

                    // Write to cache
                    await stream.CopyToAsync(cacheStream);
                }
            }
            catch (IOException)
            {
                // Cache already exists
            }

            return await GetAsync(name);
        }

        public async Task<Stream> GetAsync(string name)
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

        string getPath(string name) => Path.Combine(CachePath, processName(name));
        static string processName(string name) => name.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    }
}