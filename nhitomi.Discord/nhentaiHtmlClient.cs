// Copyright (c) 20198phosphene47
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
    public static class nhentaiHtml
    {
        public static string Gallery(int id) => $"https://nhentai.net/g/{id}";
        public static string All(int index = 0) => $"https://nhentai.net/?page={index + 1}";
        public static string Search(string query, int index = 0) => $"https://nhentai.net/search/?q={query}&page={index + 1}";

        public static class XPath
        {
            public const string CoverImage = @"//*[@id=""cover""]/a/img";
            public const string UploadDate = @"//*[@id=""info""]/h1";
            public const string PrettyName = @"//*[@id=""info""]/h1";
            public const string JapaneseName = @"//*[@id=""info""]/h2";
            public const string ThumbImage = @"//*[@id=""thumbnail-container""]/div/a/img";
            public const string TagAnchor = @"//*[@class=""tags""]/a";
            public const string SearchItem = @"//*[@class=""gallery""]/a";
        }
    }

    public sealed class nhentaiHtmlClient : IDoujinClient
    {
        public string Name => "nhentai";
        public string Url => "https://nhentai.net/";
        public string IconUrl => "https://cdn.cybrhome.com/media/website/live/icon/icon_nhentai.net_57f740.png";

        public DoujinClientMethod Method => DoujinClientMethod.Html;

        public Regex GalleryRegex { get; } = new Regex(nhentai.GalleryRegex, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        readonly IMemoryCache _cache;
        readonly HttpClient _http;
        readonly JsonSerializer _json;
        readonly ILogger _logger;

        public nhentaiHtmlClient(
            IHttpClientFactory httpFactory,
            IMemoryCache cache,
            JsonSerializer json,
            ILogger<nhentaiHtmlClient> logger
        )
        {
            _http = httpFactory?.CreateClient(Name);
            _cache = cache;
            _json = json;
            _logger = logger;
        }

        IDoujin wrap(nhentai.DoujinData data) => new nhentaiDoujin(this, data);

        Task throttle() => Task.Delay(TimeSpan.FromMilliseconds(nhentai.RequestCooldown));

        static readonly Regex _mediaIdRegex = new Regex(@"(?<=galleries\/)\d+(?=\/cover)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Regex _tagUrlRegex = new Regex(@"\/(?<type>.*)\/(?<name>.*)\/", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Regex _tagTitleRegex = new Regex(@"\[[^\]]*\]|\([^\)]*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Regex _galleryRegex = new Regex(@"(?<=g\/)\d+(?=\/)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public async Task<IDoujin> GetAsync(string id)
        {
            if (!int.TryParse(id, out var intId))
                return null;

            return wrap(
                await _cache.GetOrCreateAsync<nhentai.DoujinData>(
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

            async Task<nhentai.DoujinData> getAsync()
            {
                try
                {
                    HtmlNode root;

                    using (var response = await _http.GetAsync(nhentaiHtml.Gallery(intId)))
                    using (var reader = new StringReader(await response.Content.ReadAsStringAsync()))
                    {
                        var doc = new HtmlDocument();
                        doc.Load(reader);

                        root = doc.DocumentNode;
                    }

                    // Scrape data from HTML using XPath
                    var data = new nhentai.DoujinData
                    {
                        id = intId,
                        media_id = int.Parse(_mediaIdRegex.Match(root.SelectSingleNode(nhentaiHtml.XPath.CoverImage).Attributes["data-src"].Value).Value),
                        // TODO:
                        upload_date = 0,
                        title = new nhentai.DoujinData.Title
                        {
                            japanese = _tagTitleRegex.Replace(innerSanitized(root.SelectSingleNode(nhentaiHtml.XPath.JapaneseName)), string.Empty).Trim(),
                            pretty = _tagTitleRegex.Replace(innerSanitized(root.SelectSingleNode(nhentaiHtml.XPath.PrettyName)), string.Empty).Trim()
                        },
                        images = new nhentai.DoujinData.Images
                        {
                            pages = root
                                .SelectNodes(nhentaiHtml.XPath.ThumbImage)
                                .Select(n => new nhentai.DoujinData.Images.Image { t = n.Attributes["data-src"].Value.SubstringFromEnd(3) })
                                .ToArray()
                        },
                        tags = root
                            .SelectNodes(nhentaiHtml.XPath.TagAnchor)
                            .Select(n =>
                            {
                                var match = _tagUrlRegex.Match(n.Attributes["href"].Value);
                                return new nhentai.DoujinData.Tag
                                {
                                    type = match.Groups["type"].Value,
                                    name = match.Groups["name"].Value.Replace('-', ' ')
                                };
                            })
                            .ToArray()
                    };

                    _logger.LogDebug($"Got doujin {id}: {data.title.japanese}");

                    return data;
                }
                catch (HttpRequestException) { return null; }
                finally
                {
                    await throttle();
                }
            }
        }

        static string innerSanitized(HtmlNode node) => node == null ? null : HtmlEntity.DeEntitize(node.InnerText).Trim();

        public Task<IAsyncEnumerable<IDoujin>> SearchAsync(string query) =>
            AsyncEnumerable.CreateEnumerable(() =>
            {
                string[] current = null;
                var index = 0;

                return AsyncEnumerable.CreateEnumerator(
                    moveNext: async token =>
                    {
                        try
                        {
                            var url = string.IsNullOrWhiteSpace(query)
                                ? nhentaiHtml.All(index)
                                : nhentaiHtml.Search(query, index);

                            HtmlNode root;

                            using (var response = await _http.GetAsync(url))
                            using (var reader = new StringReader(await response.Content.ReadAsStringAsync()))
                            {
                                var doc = new HtmlDocument();
                                doc.Load(reader);

                                root = doc.DocumentNode;
                            }

                            current = root
                                .SelectNodes(nhentaiHtml.XPath.SearchItem)
                                .Select(n => _galleryRegex.Match(n.Attributes["href"].Value).Value)
                                .ToArray();

                            index++;

                            return true;
                        }
                        catch (HttpRequestException) { return false; }
                        finally
                        {
                            await throttle();
                        }
                    },
                    current: () => current,
                    dispose: () => { }
                );
            })
            .SelectMany(list => AsyncEnumerable.CreateEnumerable(() =>
            {
                IDoujin current = null;
                var index = 0;

                return AsyncEnumerable.CreateEnumerator(
                    moveNext: async token =>
                    {
                        current = await GetAsync(list[index++]);
                        return true;
                    },
                    current: () => current,
                    dispose: () => { }
                );
            }))
            .AsCompletedTask();

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

        public override string ToString() => Name;

        public void Dispose() { }
    }
}