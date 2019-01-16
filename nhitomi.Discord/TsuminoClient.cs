// Copyright (c) 2019 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace nhitomi
{
    public static class Tsumino
    {
        public const int RequestCooldown = 500;

        public const string GalleryRegex = @"\b((http|https):\/\/)?(www\.)?tsumino(\.com)?\/(Book\/Info\/)?(?<tsumino>[0-9]{1,5})\b";

        public static string Book(int id) => $"https://www.tsumino.com/Book/Info/{id}/";

        public static class XPath
        {
            public const string BookTitle = @"//*[@id=""Title""]";
            public const string BookUploader = @"//*[@id=""Uploader""]";
            public const string BookUploaded = @"//*[@id=""Uploaded""]";
            public const string BookPages = @"//*[@id=""Pages""]";
            public const string BookRating = @"//*[@id=""Rating""]";
            public const string BookCategory = @"//*[@id=""Category""]/a";
            public const string BookCollection = @"//*[@id=""Collection""]/a";
            public const string BookGroup = @"//*[@id=""Group""]/a";
            public const string BookArtist = @"//*[@id=""Artist""]/a";
            public const string BookParody = @"//*[@id=""Parody""]/a";
            public const string BookCharacter = @"//*[@id=""Character""]/a";
            public const string BookTag = @"//*[@id=""Tag""]/a";
        }

        public sealed class DoujinData
        {
            public readonly DateTime _processed = DateTime.UtcNow;

            public int id;
            public string title;
            public string uploader;
            public string uploaded;
            public int pages;

            public Rating rating;
            public struct Rating
            {
                static readonly Regex _ratingRegex = new Regex(@"(?<value>\d*\.?\d+)\s\((?<users>\d+)\susers\s\/\s(?<favs>\d+)\sfavs\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

                public Rating(string str)
                {
                    var match = _ratingRegex.Match(str);

                    value = double.Parse(match.Groups["value"].Value);
                    users = int.Parse(match.Groups["users"].Value);
                    favs = int.Parse(match.Groups["favs"].Value);
                }

                public double value;
                public int users;
                public int favs;
            }

            public string category;
            public string collection;
            public string group;
            public string artist;
            public string parody;
            public string[] characters;
            public string[] tags;
        }
    }

    public sealed class TsuminoClient : IDoujinClient
    {
        public string Name => "Tsumino";
        public string Url => "https://www.tsumino.com/";
        public string IconUrl => "https://cdn.discordapp.com/icons/167128230908657664/b2089ee1d26a7e168d63960d6ed31b66.png";

        public DoujinClientMethod Method => DoujinClientMethod.Html;

        public Regex GalleryRegex => new Regex(Tsumino.GalleryRegex, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        readonly IMemoryCache _cache;
        readonly HttpClient _http;
        readonly JsonSerializer _json;
        readonly ILogger _logger;

        public TsuminoClient(
            IHttpClientFactory httpFactory,
            IMemoryCache cache,
            JsonSerializer json,
            ILogger<TsuminoClient> logger
        )
        {
            _http = httpFactory?.CreateClient(Name);
            _cache = cache;
            _json = json;
            _logger = logger;
        }

        IDoujin wrap(Tsumino.DoujinData data) => new TsuminoDoujin(this, data);

        Task throttle() => Task.Delay(TimeSpan.FromMilliseconds(Tsumino.RequestCooldown));

        public async Task<IDoujin> GetAsync(string id)
        {
            if (!int.TryParse(id, out var intId))
                return null;

            return wrap(
                await _cache.GetOrCreateAsync<Tsumino.DoujinData>(
                    key: $"{Name}/{id}",
                    factory: async entry =>
                    {
                        try
                        {
                            entry.AbsoluteExpirationRelativeToNow = DoujinCacheOptions.Expiration;
                            return await getAsync();
                        }
                        finally
                        {
                            await throttle();
                        }
                    }
                )
            );

            async Task<Tsumino.DoujinData> getAsync()
            {
                try
                {
                    HtmlNode root;

                    using (var response = await _http.GetAsync(Tsumino.Book(intId)))
                    using (var reader = new StringReader(await response.Content.ReadAsStringAsync()))
                    {
                        var doc = new HtmlDocument();
                        doc.Load(reader);

                        root = doc.DocumentNode;
                    }

                    // Scrape data from HTML using XPath
                    var data = new Tsumino.DoujinData
                    {
                        id = intId,
                        title = innerSanitized(root.SelectSingleNode(Tsumino.XPath.BookTitle)),
                        uploader = innerSanitized(root.SelectSingleNode(Tsumino.XPath.BookUploader)),
                        uploaded = innerSanitized(root.SelectSingleNode(Tsumino.XPath.BookUploaded)),
                        pages = int.Parse(innerSanitized(root.SelectSingleNode(Tsumino.XPath.BookPages))),
                        rating = new Tsumino.DoujinData.Rating(innerSanitized(root.SelectSingleNode(Tsumino.XPath.BookRating))),
                        category = innerSanitized(root.SelectSingleNode(Tsumino.XPath.BookCategory)),
                        collection = innerSanitized(root.SelectSingleNode(Tsumino.XPath.BookCollection)),
                        group = innerSanitized(root.SelectSingleNode(Tsumino.XPath.BookGroup)),
                        artist = innerSanitized(root.SelectSingleNode(Tsumino.XPath.BookArtist)),
                        parody = innerSanitized(root.SelectSingleNode(Tsumino.XPath.BookParody)),
                        characters = root.SelectNodes(Tsumino.XPath.BookCharacter)?.Select(innerSanitized).ToArray(),
                        tags = root.SelectNodes(Tsumino.XPath.BookTag)?.Select(innerSanitized).ToArray()
                    };

                    _logger.LogDebug($"Got doujin {id}: {data.title}");

                    return data;
                }
                catch (HttpRequestException) { return null; }
            }
        }

        static string innerSanitized(HtmlNode node) => node == null ? null : HtmlEntity.DeEntitize(node.InnerText).Trim();

        public Task<IAsyncEnumerable<IDoujin>> SearchAsync(string query)
        {
            throw new System.NotImplementedException();
        }

        public async Task<Stream> GetStreamAsync(string url)
        {
            try
            {
                return await _http.GetStreamAsync(url);
            }
            finally
            {
                await throttle();
            }
        }

        public Task UpdateAsync() => Task.CompletedTask;

        public void Dispose() { }
    }
}