using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using nhitomi.Database;
using nhitomi.Models;
using nhitomi.Scrapers.Tests;
using nhitomi.Storage;

namespace nhitomi.Scrapers
{
    public class nhentaiBook
    {
        [JsonProperty("id")] public int Id;
        [JsonProperty("media_id")] public int MediaId;
        [JsonProperty("upload_date")] public long UploadDate;
        [JsonProperty("title")] public TitleData Title;
        [JsonProperty("images")] public ImagesData Images;
        [JsonProperty("tags")] public TagData[] Tags;

        public class TitleData
        {
            [JsonProperty("japanese")] public string Japanese;
            [JsonProperty("english")] public string English;
        }

        public class ImagesData
        {
            [JsonProperty("pages")] public ImageData[] Pages;

            public class ImageData
            {
                [JsonProperty("t")] public char Type;
            }
        }

        public class TagData
        {
            [JsonProperty("type")] public string Type;
            [JsonProperty("name")] public string Name;
        }

        // regex to match any () and [] in titles
        static readonly Regex _bracketsRegex = new Regex(@"\([^)]*\)|\[[^\]]*\]", RegexOptions.Compiled | RegexOptions.Singleline);

        // regex to match the convention in title
        static readonly Regex _conventionRegex = new Regex(@"^\((?<convention>.*?)\)", RegexOptions.Compiled | RegexOptions.Singleline);

        static string FixTitle(string s)
        {
            if (string.IsNullOrEmpty(s))
                return null;

            // replace stuff in brackets with nothing
            s = _bracketsRegex.Replace(s, "").Trim();

            return string.IsNullOrEmpty(s) ? null : HttpUtility.HtmlDecode(s);
        }

        static string FindConvention(string s)
        {
            if (string.IsNullOrEmpty(s))
                return null;

            return _conventionRegex.Match(s.TrimStart()).Groups["convention"].Value;
        }

        public DbBook Convert()
        {
            var japanese = FixTitle(Title.Japanese);
            var english  = FixTitle(Title.English);

            return new DbBook
            {
                PrimaryName = japanese ?? english,
                EnglishName = english ?? japanese,

                TagsGeneral    = Tags?.Where(t => t.Type == "tag").ToArray(t => t.Name),
                TagsArtist     = Tags?.Where(t => t.Type == "artist").ToArray(t => t.Name),
                TagsParody     = Tags?.Where(t => t.Type == "parody" && t.Name != "original").ToArray(t => t.Name),
                TagsCharacter  = Tags?.Where(t => t.Type == "character").ToArray(t => t.Name),
                TagsConvention = new[] { FindConvention(english ?? japanese) },
                TagsCircle     = Tags?.Where(t => t.Type == "group").ToArray(t => t.Name),

                Category = Enum.TryParse<BookCategory>(Tags?.FirstOrDefault(t => t.Type == "category")?.Name, true, out var cat) ? cat : BookCategory.Doujinshi,
                Contents = new[]
                {
                    new DbBookContent
                    {
                        Language = Enum.TryParse<LanguageType>(Tags?.FirstOrDefault(t => t.Type == "language" && t.Name != "translated")?.Name, true, out var lang) ? lang : LanguageType.Japanese,
                        Pages    = Images.Pages.ToArray(p => new DbBookImage()),
                        Source   = ScraperType.nhentai,
                        SourceId = Id.ToString(),
                        Data     = ""
                    }
                }
            };
        }
    }

    public class nhentaiList
    {
        [JsonProperty("result")] public nhentaiBook[] Results;
        [JsonProperty("num_pages")] public int NumPages;
        [JsonProperty("per_page")] public int PerPage;
    }

    public class nhentaiScraperOptions : ScraperOptions
    {
        /// <summary>
        /// Books are indexed reverse-chronologically.
        /// Specifies the number of additional books to index after indexing all newer books since the last indexed book.
        /// </summary>
        public int AdditionalScrapeItems { get; set; } = 20;

        /// <summary>
        /// Minimum upload time of books to be indexed, which is year 2016 by default (approx. ID 153000).
        /// </summary>
        public DateTime MinimumUploadTime { get; set; } = new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    public class nhentaiScraper : BookScraperBase
    {
        readonly HttpClient _http;
        readonly IOptionsMonitor<nhentaiScraperOptions> _options;
        readonly ILogger<nhentaiScraper> _logger;
        readonly IStorage _storage;

        public override ScraperType Type => ScraperType.nhentai;
        public override ScraperUrlRegex UrlRegex { get; } = new ScraperUrlRegex(@"(nh(entai)?(\/|\s+)|(https?:\/\/)?nhentai\.net\/g\/)(?<id>\d{1,6})\/?");
        public override IScraperTestManager TestManager { get; }

        public nhentaiScraper(IServiceProvider services, IOptionsMonitor<nhentaiScraperOptions> options, ILogger<nhentaiScraper> logger, IHttpClientFactory http, IStorage storage) : base(services, options, logger)
        {
            _options = options;
            _logger  = logger;
            _http    = http.CreateClient(nameof(nhentaiScraper));
            _storage = storage;

            TestManager = new ScraperTestManager<nhentaiBook>(this);
        }

        sealed class State
        {
            public int? LastUpper { get; set; }
            public int? LastLower { get; set; }
        }

        protected override async IAsyncEnumerable<DbBook> ScrapeAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var options = _options.CurrentValue;

            var state = await GetStateAsync<State>(cancellationToken) ?? new State();

            if (state.LastUpper == null)
            {
                // use upper as latest book
                state.LastUpper = (await EnumerateAsync(null, cancellationToken).FirstOrDefaultAsync(cancellationToken))?.Id;

                if (state.LastUpper == null)
                    yield break;
            }
            else
            {
                var latest = null as nhentaiBook;

                // return new books since upper
                await foreach (var book in EnumerateAsync(null, cancellationToken))
                {
                    if (!FilterBook(book))
                        break;

                    yield return book.Convert();

                    latest ??= book;
                }

                // set upper as latest book
                if (latest != null)
                    state.LastUpper = latest.Id;
            }

            state.LastLower ??= state.LastUpper;

            // find additional books on top of new books
            if (state.LastLower != null)
            {
                var count = 0;

                await foreach (var book in EnumerateAsync(state.LastLower + 1, cancellationToken))
                {
                    if (!FilterBook(book))
                        break;

                    yield return book.Convert();

                    state.LastLower = book.Id;

                    if (++count >= options.AdditionalScrapeItems)
                        break;
                }
            }

            await SetStateAsync(state, cancellationToken);
        }

        bool FilterBook(nhentaiBook book)
            => DateTimeOffset.FromUnixTimeSeconds(book.UploadDate).UtcDateTime >= _options.CurrentValue.MinimumUploadTime;

        /// <summary>
        /// Retrieves a book by ID.
        /// </summary>
        public async Task<nhentaiBook> GetAsync(int id, CancellationToken cancellationToken = default)
        {
            using var response = await _http.GetAsync($"https://nhentai.net/api/gallery/{id}", cancellationToken);

            // some books may be missing
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            return JsonConvert.DeserializeObject<nhentaiBook>(await response.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// Enumerates all books reverse-chronologically (descending ID).
        /// </summary>
        public async IAsyncEnumerable<nhentaiBook> EnumerateAsync(int? start, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var encountered = new HashSet<int>();

            // if no start is specified, we can use all listing with pagination
            if (start == null)
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

            // otherwise fetch each item individually
            else
                for (var id = start.Value;; id--)
                {
                    var book = await GetAsync(id, cancellationToken);

                    // some books may be missing
                    if (book == null)
                        continue;

                    if (encountered.Add(book.Id))
                        yield return book;
                }
        }
    }
}