using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace nhitomi.Core.Clients.Hitomi
{
    public static class Hitomi
    {
        public static string Gallery(int id) =>
            $"https://hitomi.la/galleries/{id}.html";

        public static string GalleryInfo(int id, char? server = null) =>
            $"https://{server}tn.hitomi.la/galleries/{id}.js";

        static char GetCdn(int id) =>
            (char) ('a' + (id % 10 == 1 ? 0 : id) % 2);

        public static string Image(int id, string name) =>
            $"https://{GetCdn(id)}a.hitomi.la/galleries/{id}/{name}";

        public static class XPath
        {
            const string _gallery = "//div[contains(@class,'gallery')]";
            const string _galleryInfo = "//div[contains(@class,'gallery-info')]";

            public const string Name = _gallery + "//a[contains(@href,'/reader/')]";
            public const string Artists = _gallery + "//a[contains(@href,'/artist/')]";
            public const string Groups = _gallery + "//a[contains(@href,'/group/')]";
            public const string Type = _gallery + "//a[contains(@href,'/type/')]";
            public const string Language = _galleryInfo + "//tr[3]//a";
            public const string Series = _gallery + "//a[contains(@href,'/series/')]";
            public const string Tags = _gallery + "//a[contains(@href,'/tag/')]";
            public const string Characters = _gallery + "//a[contains(@href,'/character/')]";
            public const string Date = _gallery + "//span[contains(@class,'date')]";
        }

        public const string NozomiIndex = "https://ltn.hitomi.la/index-all.nozomi";
    }

    public sealed class HitomiClient : IDoujinClient
    {
        public string Name => nameof(Hitomi);
        public string Url => "https://hitomi.la/";

        readonly IHttpClient _http;
        readonly JsonSerializer _serializer;
        readonly ILogger<HitomiClient> _logger;

        public HitomiClient(IHttpClient http, JsonSerializer serializer, ILogger<HitomiClient> logger)
        {
            _http = http;
            _serializer = serializer;
            _logger = logger;
        }

        // regex to match () and [] in titles
        static readonly Regex _bracketsRegex = new Regex(@"\([^)]*\)|\[[^\]]*\]",
            RegexOptions.Compiled | RegexOptions.Singleline);

        // regex to match index-language-page
        static readonly Regex _languageHrefRegex = new Regex(@"index-(?<language>\w+)-\d+",
            RegexOptions.Compiled | RegexOptions.Singleline);

        public async Task<DoujinInfo> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            if (!int.TryParse(id, out var intId))
                return null;

            HtmlNode root;

            // load html page
            using (var response = await _http.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(Hitomi.Gallery(intId))
            }, cancellationToken))
            {
                if (!response.IsSuccessStatusCode)
                    return null;

                using (var reader = new StringReader(await response.Content.ReadAsStringAsync()))
                {
                    var doc = new HtmlDocument();
                    doc.Load(reader);

                    root = doc.DocumentNode;
                }
            }

            // filter out anime
            var type = Sanitize(root.SelectSingleNode(Hitomi.XPath.Type));

            if (type != null && type.Equals("anime", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation($"Skipping '{id}' because it is of type 'anime'.");
                return null;
            }

            var prettyName = Sanitize(root.SelectSingleNode(Hitomi.XPath.Name));

            // replace stuff in brackets with nothing
            prettyName = _bracketsRegex.Replace(prettyName, "");

            var originalName = prettyName;

            // parse names with two parts
            var pipeIndex = prettyName.IndexOf('|');
            if (pipeIndex != -1)
            {
                prettyName = prettyName.Substring(0, pipeIndex).Trim();
                originalName = originalName.Substring(pipeIndex + 1).Trim();
            }

            // parse language
            var languageHref = root.SelectSingleNode(Hitomi.XPath.Language)?.Attributes["href"]?.Value;
            var language = languageHref == null
                ? null
                : _languageHrefRegex.Match(languageHref).Groups["language"].Value;

            var doujin = new DoujinInfo
            {
                GalleryUrl = $"https://hitomi.la/galleries/{id}.html",

                PrettyName = prettyName,
                OriginalName = originalName,

                UploadTime = DateTime.Parse(Sanitize(root.SelectSingleNode(Hitomi.XPath.Date))).ToUniversalTime(),

                Source = this,
                SourceId = id,

                Artist = Sanitize(root.SelectSingleNode(Hitomi.XPath.Artists))?.ToLowerInvariant(),
                Group = Sanitize(root.SelectSingleNode(Hitomi.XPath.Groups))?.ToLowerInvariant(),
                Language = language?.ToLowerInvariant(),
                Parody = ConvertSeries(Sanitize(root.SelectSingleNode(Hitomi.XPath.Series)))?.ToLowerInvariant(),
                Characters = root.SelectNodes(Hitomi.XPath.Characters)?.Select(n => Sanitize(n)?.ToLowerInvariant()),
                Tags = root.SelectNodes(Hitomi.XPath.Tags)?.Select(n => ConvertTag(Sanitize(n)?.ToLowerInvariant()))
            };

            // parse images
            using (var response = await _http.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(Hitomi.GalleryInfo(intId))
            }, cancellationToken))
            {
                if (!response.IsSuccessStatusCode)
                    return null;

                using (var textReader = new StringReader(await response.Content.ReadAsStringAsync()))
                using (var jsonReader = new JsonTextReader(textReader))
                {
                    // discard javascript bit and start at json
                    while ((char) textReader.Peek() != '[')
                        textReader.Read();

                    var images = _serializer.Deserialize<ImageInfo[]>(jsonReader);

                    doujin.PageCount = images.Length;
                    doujin.Data = _serializer.Serialize(new InternalDoujinData
                    {
                        Id = intId,
                        Images = images.Select(i => i.Name).ToArray()
                    });
                }
            }

            return doujin;
        }

        sealed class InternalDoujinData
        {
            public int Id;
            public string[] Images;
        }

        static string ConvertSeries(string series) =>
            series == null || series.Equals("original", StringComparison.OrdinalIgnoreCase) ? null : series;

        static string ConvertTag(string tag) =>
            tag.Contains(':') ? tag.Substring(tag.IndexOf(':') + 1) : tag.TrimEnd('♀', '♂', ' ');

        static string Sanitize(HtmlNode node)
        {
            if (node == null)
                return null;

            var text = HtmlEntity.DeEntitize(node.InnerText).Trim();

            return string.IsNullOrEmpty(text) ? null : text;
        }

        struct ImageInfo
        {
            [JsonProperty("name")] public string Name;
            [JsonProperty("width")] public int Width;
            [JsonProperty("height")] public int Height;
        }

        async Task<int[]> ReadNozomiIndicesAsync(CancellationToken cancellationToken = default)
        {
            using (var memory = new MemoryStream())
            {
                using (var response = await _http.SendAsync(new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(Hitomi.NozomiIndex)
                }, cancellationToken))
                {
                    if (!response.IsSuccessStatusCode)
                        return null;

                    using (var stream = await response.Content.ReadAsStreamAsync())
                        await stream.CopyToAsync(memory, 4096, cancellationToken);

                    memory.Position = 0;
                }

                var indices = new int[memory.Length / sizeof(int)];

                using (var reader = new BinaryReader(memory))
                {
                    for (var i = 0; i < indices.Length; i++)
                        indices[i] = reader.ReadInt32Be();
                }

                return indices;
            }
        }

        public async Task<IEnumerable<string>> EnumerateAsync(string startId = null,
            CancellationToken cancellationToken = default)
        {
            var indices = await ReadNozomiIndicesAsync(cancellationToken);

            if (indices == null)
                return null;

            Array.Sort(indices);

            // skip to starting id
            int.TryParse(startId, out var intId);

            var startIndex = 0;

            for (; startIndex < indices.Length; startIndex++)
                if (indices[startIndex] >= intId)
                    break;

            indices = indices.Subarray(startIndex);

            return indices.Select(x => x.ToString());
        }

        public IEnumerable<string> PopulatePages(Doujin doujin)
        {
            var data = _serializer.Deserialize<InternalDoujinData>(doujin.Data);

            if (data.Images == null)
                yield break;

            foreach (var image in data.Images)
                yield return Hitomi.Image(data.Id, image);
        }

        public void Dispose()
        {
        }
    }
}