// Copyright (c) 2018 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using HtmlAgilityPack;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace nhitomi
{
    public static class Hitomi
    {
        public static string Gallery(int id) => $"https://hitomi.la/galleries/{id}.html";
        public static string GalleryInfo(int id, char? server = null) => $"https://{server}tn.hitomi.la/galleries/{id}.js";

        public static char GetCdn(int id) => (char)('a' + (id % 10 == 1 ? 0 : id) % 2);

        public static string Image(int id, string name) => $"https://{GetCdn(id)}a.hitomi.la/galleries/{id}/{name}";
        public static string CoverImage(int id, string name) => $"https://{GetCdn(id)}tn.hitomi.la/bigtn/{id}/{name}.jpg";
        public static string ThumbImage(int id, string name) => $"https://{GetCdn(id)}tn.hitomi.la/smalltn/{id}/{name}.jpg";

        public static string Chunk(int index) => $"https://ltn.hitomi.la/galleries{index}.json";
        public static string List(string category = null, string name = "index", string language = "all", int page = 1) =>
            $"https://hitomi.la/{category ?? category + "/"}{name}-{language}-{page}.html";

        public static class XPath
        {
            public const string _Gallery = "//div[contains(@class,'gallery')]";
            public const string _GalleryInfo = "//div[contains(@class,'gallery-info')]";
            public const string _GalleryContent = "//div[contains(@class,'gallery-content')]";

            public const string Name = _Gallery + "//a[contains(@href,'/reader/')]";
            public const string Artists = _Gallery + "//a[contains(@href,'/artist/')]";
            public const string Groups = _Gallery + "//a[contains(@href,'/group/')]";
            public const string Type = _Gallery + "//a[contains(@href,'/type/')]";
            public const string Language = _GalleryInfo + "//tr[3]//a";
            public const string Series = _Gallery + "//a[contains(@href,'/series/')]";
            public const string Tags = _Gallery + "//a[contains(@href,'/tag/')]";
            public const string Characters = _Gallery + "//a[contains(@href,'/character/')]";
            public const string Date = _Gallery + "//span[contains(@class,'date')]";

            public const string Item = _GalleryContent + "//a[contains(@href,'/galleries/')]";
        }
    }

    public sealed class HitomiClient : IDoujinClient
    {
        public string Name => nameof(Hitomi);
        public string Url => "https://hitomi.la/";
        public string IconUrl => "https://ltn.hitomi.la/favicon-160x160.png";

        public Regex GalleryRegex { get; } = new Regex(@"((http|https):\/\/)?hitomi(.la)?\/(galleries\/)?(?<hitomi>[0-9]{1,7})", RegexOptions.Compiled);

        readonly IMemoryCache _cache;
        readonly HttpClient _http;
        readonly JsonSerializer _json;
        readonly ILogger _logger;

        public HitomiClient(
            IHttpClientFactory httpFactory,
            IMemoryCache cache,
            JsonSerializer json,
            ILogger<HitomiClient> logger
        )
        {
            _http = httpFactory?.CreateClient(Name);
            _cache = cache;
            _json = json;
            _logger = logger;
        }

        internal sealed class DoujinData
        {
            public readonly DateTime _processed = DateTime.UtcNow;

            public int id;

            public string name;
            public string[] artists;
            public string[] groups;
            public string type;
            public string language;
            public string series;
            public string[] characters;
            public string date;

            public Image[] images;
            public struct Image
            {
                public string name;
                public int width;
                public int height;
            }

            public Tag[] tags;
            public struct Tag
            {
                public static Tag Parse(string str) => new Tag
                {
                    Value = str.Contains(':')
                        ? str.Substring(str.IndexOf(':') + 1)
                        : str.TrimEnd('♀', '♂', ' '),
                    Sex = str.Contains(':')
                        ? str[0] == 'm' ? '♀' : str[0] == 'f' ? '♂' : (char?)null
                        : str.EndsWith('♀') ? '♀' : str.EndsWith('♂') ? '♂' : (char?)null
                };

                public string Value;
                public char? Sex;
            }
        }

        IDoujin wrap(DoujinData data) => new HitomiDoujin(this, data);

        public async Task<IDoujin> GetAsync(string id)
        {
            if (!int.TryParse(id, out var intId))
                return null;

            return wrap(
                await _cache.GetOrCreateAsync<DoujinData>(
                    key: $"{Name}/{id}",
                    factory: entry =>
                    {
                        entry.AbsoluteExpirationRelativeToNow = DoujinCacheOptions.Expiration;
                        return getAsync();
                    }
                )
            );

            async Task<DoujinData> getAsync()
            {
                try
                {
                    DoujinData data;

                    using (var response = await _http.GetAsync(Hitomi.Gallery(intId)))
                    using (var reader = new StringReader(await response.Content.ReadAsStringAsync()))
                    {
                        var doc = new HtmlDocument();
                        doc.Load(reader);

                        var root = doc.DocumentNode;

                        // Scrape data from HTML using XPath
                        data = new DoujinData
                        {
                            id = intId,
                            name = root.SelectSingleNode(Hitomi.XPath.Name)?.InnerText.Trim(),
                            artists = root.SelectNodes(Hitomi.XPath.Artists)?.Select(n => n.InnerText.Trim()).ToArray(),
                            groups = root.SelectNodes(Hitomi.XPath.Groups)?.Select(n => n.InnerText.Trim()).ToArray(),
                            language = root.SelectSingleNode(Hitomi.XPath.Language)?.InnerText.Trim(),
                            series = root.SelectSingleNode(Hitomi.XPath.Series)?.InnerText.Trim(),
                            tags = root.SelectNodes(Hitomi.XPath.Tags)?.Select(n => DoujinData.Tag.Parse(n.InnerText.Trim())).ToArray(),
                            characters = root.SelectNodes(Hitomi.XPath.Characters)?.Select(n => n.InnerText.Trim()).ToArray(),
                            date = root.SelectSingleNode(Hitomi.XPath.Date)?.InnerText.Trim()
                        };

                        // We don't want anime
                        var type = root.SelectSingleNode(Hitomi.XPath.Type)?.InnerText.Trim();
                        if (type == null || type.Equals("anime", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogWarning($"Skipping {id} because it is of type 'anime'.");
                            return null;
                        }
                    }

                    using (var response = await _http.GetAsync(Hitomi.GalleryInfo(intId)))
                    using (var textReader = new StringReader(await response.Content.ReadAsStringAsync()))
                    using (var jsonReader = new JsonTextReader(textReader))
                    {
                        // Discard javascript and start at actual json
                        while ((char)textReader.Peek() != '[')
                            textReader.Read();

                        data.images = _json.Deserialize<DoujinData.Image[]>(jsonReader);
                    }

                    _logger.LogDebug($"Got doujin {id}: {data.name}");

                    return data;
                }
                catch (HttpRequestException) { return null; }
            }
        }

        readonly HashSet<ChunkItemData> _db = new HashSet<ChunkItemData>();
        internal struct ChunkItemData
        {
            public readonly int id;
            public readonly string name;
            public readonly string[] tags;
            public readonly string author;

            public ChunkItemData(
                int i,
                string n,
                string[] t,
                string a
            )
            {
                id = i;
                name = n;
                tags = t;
                author = a;
            }

            public override bool Equals(object obj) => obj is ChunkItemData other ? id == other.id : false;
            public override int GetHashCode() => id;
            public override string ToString() => name;
        }

        async Task updateDbAsync(int chunkIndex = 0)
        {
            try
            {
                var db = _db;

                var loadCount = 0;
                double[] elapsed;

                // Manually read tokens for best possible performance
                using (Extensions.Measure(out elapsed))
                using (var stream = await _http.GetStreamAsync(Hitomi.Chunk(chunkIndex)))
                using (var textReader = new StreamReader(stream))
                using (var jsonReader = new JsonTextReader(textReader))
                    while (await jsonReader.ReadAsync())
                    {
                        if (jsonReader.TokenType != JsonToken.StartObject)
                            continue;

                        var id = -1;
                        var name = (string)null;
                        var type = (string)null;
                        var tags = new List<string>();
                        var author = (string)null;

                        string property = null;
                        while (await jsonReader.ReadAsync())
                            switch (jsonReader.TokenType)
                            {
                                case JsonToken.PropertyName:
                                    property = (string)jsonReader.Value;
                                    break;
                                case JsonToken.Integer:
                                    switch (property)
                                    {
                                        case "id": id = unchecked((int)(long)jsonReader.Value); break;
                                    }
                                    break;
                                case JsonToken.String:
                                    switch (property)
                                    {
                                        case "n": name = (string)jsonReader.Value; break;
                                        case "type": type = (string)jsonReader.Value; break;
                                    }
                                    break;
                                case JsonToken.StartArray:
                                    switch (property)
                                    {
                                        case "t":
                                            while (await jsonReader.ReadAsync())
                                                switch (jsonReader.TokenType)
                                                {
                                                    case JsonToken.String:
                                                        tags.Add(DoujinData.Tag.Parse((string)jsonReader.Value).Value);
                                                        break;
                                                    case JsonToken.EndArray:
                                                        goto endTags;
                                                }
                                            endTags:
                                            break;
                                        case "a":
                                            await jsonReader.ReadAsync();
                                            author = (string)jsonReader.Value;

                                            while (await jsonReader.ReadAsync())
                                                if (jsonReader.TokenType == JsonToken.EndArray)
                                                    break;
                                            break;
                                    }
                                    break;
                                case JsonToken.EndObject:
                                    goto endItem;
                            }
                        endItem:

                        if (id < 0 ||
                            string.IsNullOrWhiteSpace(name) ||
                            string.IsNullOrWhiteSpace(author) ||
                            type.Equals("anime", StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (db.Add(new ChunkItemData(id, name, tags.ToArray(), author)))
                            ++loadCount;
                    }

                _logger.LogDebug($"Loaded {loadCount} new items from chunk {chunkIndex} in {elapsed.Format()}.");
            }
            catch (HttpRequestException) { }
        }

        public int ChunkLoadCount { get; set; } = 3;

        public async Task UpdateAsync()
        {
            for (var i = 0; i < ChunkLoadCount; i++)
                await updateDbAsync(i);
        }

        public Task<IAsyncEnumerable<IDoujin>> SearchAsync(string query)
        {
            IEnumerable<int> filtered;

            if (string.IsNullOrWhiteSpace(query))
                filtered = _db
                    .OrderByDescending(d => d.id)
                    .Select(d => d.id);
            else
            {
                var keywords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                int matches(string str) => matchTags(str.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                int matchTags(string[] array) => keywords.Intersect(array).Count();

                filtered = _db
                    .OrderByDescending(d => matches(d.name))
                    .ThenByDescending(d => matches(d.author))
                    .ThenByDescending(d => matchTags(d.tags))
                    .ThenByDescending(d => d.id)
                    .TakeWhile(d => matches(d.name) > 0 && matches(d.author) > 0 && matchTags(d.tags) > 0)
                    .Select(d => d.id);
            }

            return AsyncEnumerable.CreateEnumerable(() =>
            {
                var enumerator = filtered.GetEnumerator();
                IDoujin current = null;

                return AsyncEnumerable.CreateEnumerator(
                    moveNext: async token =>
                    {
                        if (!enumerator.MoveNext())
                            return false;

                        current = await GetAsync(enumerator.Current.ToString());
                        return true;
                    },
                    current: () => current,
                    dispose: enumerator.Dispose
                );
            })
            .AsCompletedTask();
        }

        public override string ToString() => Name;

        public void Dispose() { }
    }
}
