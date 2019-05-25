// Copyright (c) 2018-2019 chiya.dev
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

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

namespace nhitomi.Core.Clients
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
        public string IconUrl => "https://ltn.hitomi.la/favicon-160x160.png";
        public string GalleryRegex => @"\b((http|https):\/\/)?hitomi(\.la)?\/(galleries\/)?(?<Hitomi>[0-9]{1,7})\b";

        readonly IHttpClient _http;
        readonly JsonSerializer _json;
        readonly ILogger<HitomiClient> _logger;

        public HitomiClient(IHttpClient http, JsonSerializer json, ILogger<HitomiClient> logger)
        {
            _http = http;
            _json = json;
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
                Language = ConvertLanguage(Sanitize(root.SelectSingleNode(Hitomi.XPath.Language))),
                ParodyOf = ConvertSeries(Sanitize(root.SelectSingleNode(Hitomi.XPath.Series))),
                Characters = root.SelectNodes(Hitomi.XPath.Characters)?.Select(Sanitize),
                Artists = root.SelectNodes(Hitomi.XPath.Artists)?.Select(Sanitize),
                Groups = root.SelectNodes(Hitomi.XPath.Groups)?.Select(Sanitize),
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

                doujin.Images = _json.Deserialize<ImageInfo[]>(jsonReader)?.Select(i => Hitomi.Image(intId, i.Name));
            }

            return doujin;
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

        async Task<int[]> ReadNozomiIndexAsync(CancellationToken cancellationToken = default)
        {
            const string url = Hitomi.NozomiIndex;

            using (var memory = new MemoryStream())
            {
                using (var response = await _http.SendAsync(new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(url)
                }, cancellationToken))
                using (var stream = await response.Content.ReadAsStreamAsync())
                    await stream.CopyToAsync(memory, 4096, cancellationToken);

                var total = memory.Length / sizeof(int);
                var nozomi = new int[total];

                memory.Position = 0;

                using (var reader = new BinaryReader(memory))
                {
                    for (var i = 0; i < total; i++)
                        nozomi[i] = reader.ReadInt32Be();
                }

                return nozomi;
            }
        }

        public IAsyncEnumerable<string> EnumerateAsync(string id = null) =>
            AsyncEnumerable.CreateEnumerable(() =>
            {
                var indices = null as int[];
                var index = -1;

                return AsyncEnumerable.CreateEnumerator(
                    async token =>
                    {
                        if (indices == null)
                        {
                            indices = await ReadNozomiIndexAsync(token);

                            // skip to starting id
                            if (id != null && int.TryParse(id, out var intId))
                            {
                                var startIndex = System.Array.IndexOf(indices, intId);

                                if (startIndex != -1)
                                    indices = indices.Subarray(startIndex);
                            }
                        }

                        return ++index < indices.Length;
                    },
                    () => indices[index].ToString(),
                    () => indices = null);
            });

        public void Dispose()
        {
        }
    }
}