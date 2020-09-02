using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using nhitomi.Database;
using nhitomi.Scrapers.Tests;
using nhitomi.Storage;

namespace nhitomi.Scrapers
{
    public class HitomiScraperOptions : ScraperOptions
    {
        /// <summary>
        /// Number of books to index for each scrape.
        /// </summary>
        public int ScrapeItems { get; set; } = 5;
    }

    public class HitomiScraper : BookScraperBase
    {
        readonly HttpClient _http;
        readonly HitomiNozomiIndexReader _index;
        readonly IOptionsMonitor<HitomiScraperOptions> _options;

        public override string Name => "Hitomi";
        public override ScraperType Type => ScraperType.Hitomi;
        public override string Url => "https://hitomi.la";
        public override ScraperUrlRegex UrlRegex { get; } = new ScraperUrlRegex(@"(hi(tomi)?(\/|\s+)|(https?:\/\/)?hitomi\.la\/)((?<type>\w+)\/)?([^\/\s]+\-)?(?<id>\d{1,8})(\.html)?");
        public override IScraperTestManager TestManager { get; }

        public HitomiScraper(IServiceProvider services, IOptionsMonitor<HitomiScraperOptions> options, ILogger<BookScraperBase> logger, IHttpClientFactory http) : base(services, options, logger)
        {
            _http    = http.CreateClient(nameof(HitomiScraper));
            _index   = ActivatorUtilities.CreateInstance<HitomiNozomiIndexReader>(services);
            _options = options;

            _http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "*/*");
            _http.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "en");
            _http.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://hitomi.la");
            _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", ScraperAgent.GetUserAgent());

            TestManager = new ScraperTestManager<HitomiBook>(this);
        }

        static readonly char[] _titleReplaceChars = { ' ', '(', ')', '[', ']', '{', '}', '?', '/', ':', '<', '>', '"', '\'', '#', '%' };

        static string GetCombinedId(HitomiGalleryIdentity info)
        {
            var type = info.Type.ToLowerInvariant() switch
            {
                "artistcg" => "cg", // artistcg becomes cg for some reason
                _          => info.Type
            };

            var title = new StringBuilder(info.Title);

            foreach (var c in _titleReplaceChars)
                title.Replace(c, '-');

            // language can be null (gamecg)
            var other = info.LanguageLocalName == null ? info.Id.ToString() : $"{info.LanguageLocalName}-{info.Id}";

            return $"{type}/{title}-{other}".ToLowerInvariant();
        }

        public override string GetExternalUrl(DbBookContent content) => $"https://hitomi.la/{GetCombinedId(DataContainer.Deserialize(content.Data))}.html";

        public sealed class ScraperState
        {
            [JsonProperty("last_upper")] public int? LastUpper;
            [JsonProperty("last_lower")] public int? LastLower;
        }

        protected override async IAsyncEnumerable<BookAdaptor> ScrapeAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var options = _options.CurrentValue;

            var state = await GetStateAsync<ScraperState>(cancellationToken) ?? new ScraperState();
            var index = await _index.ReadAsync(cancellationToken);

            var targets = new HashSet<int>(options.ScrapeItems);

            // find new books
            if (state.LastUpper == null)
            {
                state.LastUpper = index[^1];
            }
            else
            {
                var i = Array.BinarySearch(index, state.LastUpper.Value);

                if (i < 0)
                    i = ~i;
                else
                    ++i;

                for (; targets.Count < options.ScrapeItems && i < index.Length; i++)
                {
                    var id = index[i];

                    state.LastUpper = id;
                    targets.Add(id);
                }
            }

            state.LastLower ??= state.LastUpper;

            // find additional books
            if (state.LastLower != null)
            {
                var i = Array.BinarySearch(index, state.LastLower.Value);

                if (i < 0)
                    i = ~i - 1;
                else
                    --i;

                for (; targets.Count < options.ScrapeItems && i >= 0; i--)
                {
                    var id = index[i];

                    state.LastLower = id;
                    targets.Add(id);
                }
            }

            foreach (var book in await Task.WhenAll(targets.Select(id => GetAsync(id, cancellationToken))))
            {
                if (book != null)
                    yield return new HitomiBookAdaptor(book);
            }

            await SetStateAsync(state, cancellationToken);
        }

        public static class XPaths
        {
            const string _gallery = "//div[contains(@class,'gallery')]";

            public const string Title = _gallery + "//a[contains(@href,'/reader/')]";
            public const string Artist = _gallery + "//a[contains(@href,'/artist/')]";
            public const string Group = _gallery + "//a[contains(@href,'/group/')]";
            public const string Series = _gallery + "//a[contains(@href,'/series/')]";
            public const string Tags = _gallery + "//a[contains(@href,'/tag/')]";
            public const string Characters = _gallery + "//a[contains(@href,'/character/')]";
        }

        /// <summary>
        /// Retrieves a book by ID.
        /// </summary>
        public async Task<HitomiBook> GetAsync(int id, CancellationToken cancellationToken = default)
        {
            HitomiGalleryInfo galleryInfo;

            // load gallery info
            using (var response = await _http.GetAsync($"https://ltn.hitomi.la/galleries/{id}.js", cancellationToken))
            {
                // some books may be missing
                if (response.StatusCode == HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();

                // content is javascript like "var galleryinfo = {json}"
                var script = await response.Content.ReadAsStringAsync();

                const string declare = "var galleryinfo =";

                if (!script.StartsWith(declare, StringComparison.OrdinalIgnoreCase))
                    throw new FormatException($"Could not parse Hitomi gallery information JavaScript file for {id}.");

                var json = script.Substring(declare.Length).Trim();

                galleryInfo = JsonConvert.DeserializeObject<HitomiGalleryInfo>(json);
            }

            if (galleryInfo.Type?.Equals("anime", StringComparison.OrdinalIgnoreCase) == true)
                return null;

            HtmlNode node;

            // load gallery html
            using (var response = await _http.GetAsync($"https://hitomi.la/{GetCombinedId(galleryInfo)}.html", cancellationToken))
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();

                var doc = new HtmlDocument();
                doc.LoadHtml(await response.Content.ReadAsStringAsync());

                node = doc.DocumentNode;
            }

            static string getText(HtmlNode n)
            {
                if (n == null)
                    return null;

                var text = HtmlEntity.DeEntitize(n.InnerText).Trim();

                return string.IsNullOrEmpty(text) ? null : text;
            }

            // scrape from html
            return new HitomiBook(galleryInfo)
            {
                Title      = getText(node.SelectSingleNode(XPaths.Title)),
                Artist     = getText(node.SelectSingleNode(XPaths.Artist)),
                Group      = getText(node.SelectSingleNode(XPaths.Group)),
                Series     = getText(node.SelectSingleNode(XPaths.Series)),
                Characters = node.SelectNodes(XPaths.Characters)?.ToArray(getText),
                Tags       = node.SelectNodes(XPaths.Tags)?.ToArray(getText)
            };
        }

        public override async Task<BookAdaptor> RetrieveAsync(DbBookContent content, CancellationToken cancellationToken = default)
        {
            var data = DataContainer.Deserialize(content.Data);
            var book = await GetAsync(data.Id, cancellationToken);

            return book == null ? null : new HitomiBookAdaptor(book);
        }

        public sealed class DataContainer : HitomiGalleryIdentity
        {
            public static string Serialize(DataContainer data) => JsonConvert.SerializeObject(data);
            public static DataContainer Deserialize(string data) => JsonConvert.DeserializeObject<DataContainer>(data);

            /// <summary>
            /// Hashes have file extensions.
            /// </summary>
            [JsonProperty("hashes")] public string[] Hashes;

            public static string CompressHash(string hash)
            {
                static int value(char hex)
                {
                    var v = (int) hex;

                    return v - (v < 58 ? 48 : (v < 97 ? 55 : 87));
                }

                // hashes are hex; they can be stored more efficiently internally with base64
                var buffer = new byte[hash.Length / 2];

                for (var i = 0; i < hash.Length >> 1; i++)
                    buffer[i] = (byte) ((value(hash[i << 1]) << 4) + (value(hash[(i << 1) + 1])));

                return Convert.ToBase64String(buffer).TrimEnd('=');
            }

            public static string DecompressHash(string hash)
            {
                switch (hash.Length % 4)
                {
                    case 2:
                        hash += "==";
                        break;

                    case 3:
                        hash += "=";
                        break;
                }

                var buffer = Convert.FromBase64String(hash);
                var chars  = new char[buffer.Length * 2];

                for (var i = 0; i < buffer.Length; i++)
                {
                    var b = buffer[i] >> 4;

                    // https://stackoverflow.com/a/14333437
                    chars[i * 2]     = (char) (87 + b + (((b - 10) >> 31) & -39));
                    b                = buffer[i] & 0xF;
                    chars[i * 2 + 1] = (char) (87 + b + (((b - 10) >> 31) & -39));
                }

                return new string(chars);
            }
        }

        // https://ltn.hitomi.la/common.js subdomain_from_galleryid
        public static char SubdomainFromGalleryId(int id, int frontends = 3) => (char) ('a' + id % frontends);

        // https://ltn.hitomi.la/common.js full_path_from_hash
        public static string FullPathFromHash(string hash)
        {
            if (hash.Length < 3)
                return hash;

            return $"{hash.Substring(hash.Length - 1, 1)}/{hash.Substring(hash.Length - 3, 2)}/{hash}";
        }

        public override async Task<StorageFile> GetImageAsync(DbBookContent content, int index, CancellationToken cancellationToken = default)
        {
            var data = DataContainer.Deserialize(content.Data);

            if (index < -1 || index >= data.Hashes.Length)
                return null;

            var hash = data.Hashes[Math.Max(0, index)];
            var ext  = Path.GetExtension(hash);

            hash = DataContainer.DecompressHash(hash.Substring(0, hash.Length - ext.Length)); // substr instead of GetFileNameWithoutExtension because hash has slashes

            // https://ltn.hitomi.la/common.js subdomain_from_url
            var galleryId = Convert.ToInt32(hash.Substring(hash.Length - 3, 2), 16);
            var frontends = 3;

            if (galleryId < 0x30)
                frontends = 2;

            if (galleryId < 0x09)
                galleryId = 1;

            var cdn = SubdomainFromGalleryId(galleryId, frontends);
            var url = index == -1
                ? $"https://tn.hitomi.la/bigtn/{FullPathFromHash(hash)}.jpg" // it seems like hitomi thumbnails are always jpg
                : $"https://{cdn}a.hitomi.la/images/{FullPathFromHash(hash)}{ext}";

            var response = await _http.SendAsync(new HttpRequestMessage
            {
                Method     = HttpMethod.Get,
                RequestUri = new Uri(url),
                Headers =
                {
                    Referrer = new Uri($"https://hitomi.la/reader/{data.Id}.html")
                }
            }, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            response.EnsureSuccessStatusCode();

            return new StorageFile
            {
                Name      = response.RequestMessage.RequestUri.ToString(),
                MediaType = response.Content.Headers.ContentType?.MediaType,
                Stream    = await response.Content.ReadAsStreamAsync()
            };
        }
    }
}