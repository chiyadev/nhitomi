using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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
            using (var reader = new StringReader(await response.Content.ReadAsStringAsync()))
            {
                var doc = new HtmlDocument();
                doc.Load(reader);

                root = doc.DocumentNode;
            }

            // filter out anime
            var type = Sanitize(root.SelectSingleNode(Hitomi.XPath.Type));

            if (type != null && type.Equals("anime", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation($"Skipping '{id}' because it is of type 'anime'.");
                return null;
            }

            // scrape data from html using xpath
            var doujin = new DoujinInfo
            {
                GalleryUrl = $"https://hitomi.la/galleries/{id}.html",

                PrettyName = Sanitize(root.SelectSingleNode(Hitomi.XPath.Name)),
                OriginalName = Sanitize(root.SelectSingleNode(Hitomi.XPath.Name)),

                UploadTime = DateTime.Parse(Sanitize(root.SelectSingleNode(Hitomi.XPath.Date))),

                Source = this,
                SourceId = id,

                Artist = Sanitize(root.SelectSingleNode(Hitomi.XPath.Artists)),
                Group = Sanitize(root.SelectSingleNode(Hitomi.XPath.Groups)),
                Language = ConvertLanguage(Sanitize(root.SelectSingleNode(Hitomi.XPath.Language))),
                Parody = ConvertSeries(Sanitize(root.SelectSingleNode(Hitomi.XPath.Series))),
                Characters = root.SelectNodes(Hitomi.XPath.Characters)?.Select(Sanitize),

                Tags = root.SelectNodes(Hitomi.XPath.Tags)?.Select(n => ConvertTag(Sanitize(n)))
            };

            // parse images
            using (var response = await _http.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(Hitomi.GalleryInfo(intId))
            }, cancellationToken))
            using (var textReader = new StringReader(await response.Content.ReadAsStringAsync()))
            using (var jsonReader = new JsonTextReader(textReader))
            {
                // discard javascript bit and start at json
                while ((char) textReader.Peek() != '[')
                    textReader.Read();

                var images = _serializer.Deserialize<ImageInfo[]>(jsonReader);

                doujin.Data = _serializer.Serialize(new InternalDoujinData
                {
                    Images = images.Select(i => i.Name).ToArray()
                });
            }

            return doujin;
        }

        sealed class InternalDoujinData
        {
            public string[] Images;
        }

        static string ConvertLanguage(string language)
        {
            switch (language)
            {
                case "日本語": return "Japanese";
                case "한국어": return "Korean";
                case "中文": return "Chinese";

                default:
                    return language;
            }
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
                using (var stream = await response.Content.ReadAsStreamAsync())
                    await stream.CopyToAsync(memory, 4096, cancellationToken);

                var indices = new int[memory.Length / sizeof(int)];

                memory.Position = 0;

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

        public void Dispose()
        {
        }
    }
}