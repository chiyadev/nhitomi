using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using nhitomi.Database;
using nhitomi.Scrapers.Tests;
using nhitomi.Storage;

namespace nhitomi.Scrapers
{
    public class nhentaiScraperOptions : ScraperOptions
    {
        /// <summary>
        /// Books are indexed reverse-chronologically.
        /// Specifies the number of additional books to index after indexing all newer books since the last indexed book.
        /// </summary>
        public int AdditionalScrapeItems { get; set; } = 5;
    }

    public class nhentaiScraper : BookScraperBase
    {
        readonly HttpClient _http;
        readonly IOptionsMonitor<nhentaiScraperOptions> _options;

        public override string Name => "nhentai";
        public override ScraperType Type => ScraperType.nhentai;
        public override string Url => "https://nhentai.net";
        public override ScraperUrlRegex UrlRegex { get; } = new ScraperUrlRegex(@"(nh(entai)?(\/|\s+)|(https?:\/\/)?nhentai\.net\/g\/)(?<id>\d{1,6})\/?");
        public override IScraperTestManager TestManager { get; }

        public nhentaiScraper(IServiceProvider services, IOptionsMonitor<nhentaiScraperOptions> options, ILogger<nhentaiScraper> logger, IHttpClientFactory http) : base(services, options, logger)
        {
            _options = options;
            _http    = http.CreateClient(nameof(nhentaiScraper));

            _http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "*/*");
            _http.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "en");
            _http.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://nhentai.net");
            _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", ScraperAgent.GetUserAgent());

            TestManager = new ScraperTestManager<nhentaiBook>(this);
        }

        public override string GetExternalUrl(DbBookContent content) => $"https://nhentai.net/g/{content.SourceId}/";

        public sealed class ScraperState
        {
            /// <summary>
            /// We iterate from the latest book to "last upper" to find new books since the last scrape.
            /// </summary>
            [JsonProperty("last_upper")] public int? LastUpper;

            /// <summary>
            /// We iterate from "last lower" a configured amount to find old books that we haven't scraped yet.
            /// </summary>
            [JsonProperty("last_lower")] public int? LastLower;
        }

        protected override async IAsyncEnumerable<BookAdaptor> ScrapeAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var options = _options.CurrentValue;

            var state = await GetStateAsync<ScraperState>(cancellationToken) ?? new ScraperState();

            if (state.LastUpper == null)
            {
                // use upper as latest book
                state.LastUpper = (await EnumerateAsync(cancellationToken).FirstOrDefaultAsync(cancellationToken))?.Id;

                if (state.LastUpper == null)
                    yield break;
            }
            else
            {
                var latest = null as nhentaiBook;

                // return new books since upper
                await foreach (var book in EnumerateAsync(cancellationToken))
                {
                    latest ??= book;

                    if (book.Id <= state.LastUpper)
                        break;

                    yield return new nhentaiBookAdaptor(book);
                }

                // set upper as latest book
                if (latest != null)
                    state.LastUpper = latest.Id;
            }

            state.LastLower ??= state.LastUpper;

            // find additional books on top of new books
            if (state.LastLower != null)
            {
                var start = state.LastLower.Value - options.AdditionalScrapeItems;

                // individually retrieve books in parallel
                var books = await Task.WhenAll(Enumerable.Range(start, options.AdditionalScrapeItems).Select(id => GetAsync(id, cancellationToken)));

                foreach (var book in books)
                    yield return new nhentaiBookAdaptor(book);

                // set lower as oldest book
                state.LastLower = start;
            }

            await SetStateAsync(state, cancellationToken);
        }

        /// <summary>
        /// Retrieves a book by ID.
        /// </summary>
        public async Task<nhentaiBook> GetAsync(int id, CancellationToken cancellationToken = default)
        {
            using var response = await _http.GetAsync($"https://nhentai.net/api/gallery/{id}", cancellationToken);

            // some books may be missing
            if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Forbidden)
                return null;

            response.EnsureSuccessStatusCode();

            return JsonConvert.DeserializeObject<nhentaiBook>(await response.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// Enumerates all books reverse-chronologically (descending ID).
        /// </summary>
        public async IAsyncEnumerable<nhentaiBook> EnumerateAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var encountered = new HashSet<int>();

            for (var page = 1;; page++)
            {
                using var response = await _http.GetAsync($"https://nhentai.net/api/galleries/all?page={page}", cancellationToken);

                response.EnsureSuccessStatusCode();

                var list = JsonConvert.DeserializeObject<nhentaiList>(await response.Content.ReadAsStringAsync());

                if (list.Results.Length == 0)
                    break;

                foreach (var book in list.Results)
                {
                    if (encountered.Add(book.Id))
                        yield return book;
                }
            }
        }

        public sealed class DataContainer
        {
            public static string Serialize(DataContainer data) => JsonConvert.SerializeObject(data);
            public static DataContainer Deserialize(string data) => JsonConvert.DeserializeObject<DataContainer>(data);

            [JsonProperty("media_id")] public int MediaId;
            [JsonProperty("ext")] public string Extensions;
        }

        public override async Task<StorageFile> GetImageAsync(DbBook book, DbBookContent content, int index, CancellationToken cancellationToken = default)
        {
            var data = DataContainer.Deserialize(content.Data);

            if (!(0 <= index && index < data.Extensions.Length))
                return null;

            var ext = ParseExtension(data.Extensions[index]);
            var url = index == -1
                ? $"https://t.nhentai.net/galleries/{data.MediaId}/thumb{ext}"
                : $"https://i.nhentai.net/galleries/{data.MediaId}/{index + 1}{ext}";

            var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            response.EnsureSuccessStatusCode();

            return new StorageFile
            {
                Name      = url,
                MediaType = response.Content.Headers.ContentType?.MediaType,
                Stream    = await response.Content.ReadAsStreamAsync()
            };
        }

        static string ParseExtension(char ext) => ext switch
        {
            'j' => ".jpg",
            'p' => ".png",
            'g' => ".gif",

            _ => ".jpg"
        };
    }
}