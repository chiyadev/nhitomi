using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace nhitomi.Scrapers
{
    public class HitomiNozomiIndexReader
    {
        readonly HttpClient _client;

        public HitomiNozomiIndexReader(HttpClient client)
        {
            _client = client;
        }

        public TimeSpan CacheExpiry { get; set; } = TimeSpan.FromMinutes(10);

        readonly struct Cache
        {
            public readonly int[] Result;
            public readonly DateTime Expiry;

            public Cache(int[] result, DateTime expiry)
            {
                Result = result;
                Expiry = expiry;
            }
        }

        Cache _cache;

        /// <summary>
        /// Reads Nozomi index data and returns all IDs in ascending order.
        /// </summary>
        public async ValueTask<int[]> ReadAsync(CancellationToken cancellationToken = default)
        {
            var cache = _cache;

            if (DateTime.UtcNow < cache.Expiry)
                return cache.Result;

            await using var memory = new MemoryStream();

            using (var response = await _client.GetAsync("https://ltn.hitomi.la/index-all.nozomi", HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                if (!response.IsSuccessStatusCode)
                    return Array.Empty<int>();

                await using (var stream = await response.Content.ReadAsStreamAsync())
                    await stream.CopyToAsync(memory, cancellationToken);

                memory.Position = 0;
            }

            var result = Process(memory.GetBuffer(), (int) memory.Length);

            _cache = new Cache(result, DateTime.UtcNow + CacheExpiry);

            return result;
        }

        static int[] Process(Span<byte> buffer, int length)
        {
            var count  = length / sizeof(int);
            var result = new int[count];

            for (var i = 0; i < count; i++)
                result[i] = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer.Slice(i * sizeof(int), sizeof(int))));

            Array.Sort(result);

            return result;
        }
    }
}