using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IO;
using Prometheus;

namespace nhitomi.Scrapers
{
    public class HitomiNozomiIndexReader
    {
        readonly HttpClient _http;
        readonly RecyclableMemoryStreamManager _memory;

        public HitomiNozomiIndexReader(IHttpClientFactory http, RecyclableMemoryStreamManager memory)
        {
            _http   = http.CreateClient(nameof(HitomiNozomiIndexReader));
            _memory = memory;
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

        static readonly Gauge _count = Metrics.CreateGauge("scraper_hitomi_index_items", "Number of items in Hitomi's book index.");

        Cache _cache;

        /// <summary>
        /// Reads Nozomi index data and returns all IDs in ascending order.
        /// </summary>
        public async ValueTask<int[]> ReadAsync(CancellationToken cancellationToken = default)
        {
            var cache = _cache;

            if (DateTime.UtcNow < cache.Expiry)
                return cache.Result;

            await using var memory = _memory.GetStream();

            using (var response = await _http.GetAsync("https://ltn.hitomi.la/index-all.nozomi", HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                if (!response.IsSuccessStatusCode)
                    return Array.Empty<int>();

                await using (var stream = await response.Content.ReadAsStreamAsync())
                    await stream.CopyToAsync(memory, cancellationToken);

                memory.Position = 0;
            }

            var result = Process(memory.GetBuffer(), (int) memory.Length);

            _cache = new Cache(result, DateTime.UtcNow + CacheExpiry);
            _count.Set(result.Length);

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