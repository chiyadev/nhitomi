// Copyright (c) 2018-2019 fate/loli
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using nhitomi.Core.Doujins;
using Newtonsoft.Json;

namespace nhitomi.Core.Clients
{
    public static class nhentaiHtml
    {
        public static string Gallery(int id) => $"https://nhentai.net/g/{id}";
        public static string All(int index = 0) => $"https://nhentai.net/?page={index + 1}";

        public static string Search(string query, int index = 0) =>
            $"https://nhentai.net/search/?q={query}&page={index + 1}";

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
        public string Name => nameof(nhentai);
        public string Url => "https://nhentai.net/";
        public string IconUrl => "https://cdn.cybrhome.com/media/website/live/icon/icon_nhentai.net_57f740.png";

        public double RequestThrottle => 500;

        public DoujinClientMethod Method => DoujinClientMethod.Html;

        public Regex GalleryRegex { get; } =
            new Regex(nhentai.GalleryRegex, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        readonly IHttpProxyClient _http;
        readonly PhysicalCache _cache;
        readonly ILogger<nhentaiHtmlClient> _logger;

        public nhentaiHtmlClient(
            IHttpProxyClient http,
            JsonSerializer json,
            ILogger<nhentaiHtmlClient> logger)
        {
            _http = http;
            _cache = new PhysicalCache(Name, json);
            _logger = logger;
        }

        static readonly Regex _mediaIdRegex = new Regex(@"(?<=galleries\/)\d+(?=\/cover)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        static readonly Regex _tagUrlRegex =
            new Regex(@"\/(?<type>.*)\/(?<name>.*)\/", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        static readonly Regex _tagTitleRegex =
            new Regex(@"\[[^\]]*\]|\([^\)]*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        static readonly Regex _galleryRegex =
            new Regex(@"(?<=g\/)\d+(?=\/)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public async Task<IDoujin> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            if (!int.TryParse(id, out var intId))
                return null;

            var data = await _cache.GetOrCreateAsync(
                id,
                token => GetAsync(intId, token),
                cancellationToken);

            return data == null
                ? null
                : new nhentaiDoujin(this, data);
        }

        async Task<nhentai.DoujinData> GetAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                HtmlNode root;

                using (var response = await _http.GetAsync(nhentaiHtml.Gallery(id), true, cancellationToken))
                using (var reader = new StringReader(await response.Content.ReadAsStringAsync()))
                {
                    var doc = new HtmlDocument();
                    doc.Load(reader);

                    root = doc.DocumentNode;
                }

                // Scrape data from HTML using XPath
                var japaneseTitleNode = root.SelectSingleNode(nhentaiHtml.XPath.JapaneseName);
                var prettyTitleNode = root.SelectSingleNode(nhentaiHtml.XPath.PrettyName);

                var data = new nhentai.DoujinData
                {
                    id = id,
                    media_id = int.Parse(_mediaIdRegex.Match(root.SelectSingleNode(nhentaiHtml.XPath.CoverImage)
                        .Attributes["data-src"].Value).Value),
                    // TODO:
                    upload_date = 0,
                    title = new nhentai.DoujinData.Title
                    {
                        japanese = japaneseTitleNode == null
                            ? null
                            : _tagTitleRegex.Replace(InnerSanitized(japaneseTitleNode), string.Empty).Trim(),
                        pretty = prettyTitleNode == null
                            ? null
                            : _tagTitleRegex.Replace(InnerSanitized(prettyTitleNode), string.Empty).Trim()
                    },
                    images = new nhentai.DoujinData.Images
                    {
                        pages = root
                            .SelectNodes(nhentaiHtml.XPath.ThumbImage)
                            .Select(n => new nhentai.DoujinData.Images.Image
                                {t = n.Attributes["data-src"].Value.SubstringFromEnd(3)})
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
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Exception while getting doujin {id}");

                return null;
            }
        }

        static string InnerSanitized(HtmlNode node) =>
            node == null ? null : HtmlEntity.DeEntitize(node.InnerText).Trim();

        public Task<IAsyncEnumerable<IDoujin>> SearchAsync(
            string query,
            CancellationToken cancellationToken = default) =>
            AsyncEnumerable.CreateEnumerable(() =>
                {
                    string[] current = null;
                    var index = 0;

                    return AsyncEnumerable.CreateEnumerator(
                        async token =>
                        {
                            try
                            {
                                var url = string.IsNullOrWhiteSpace(query)
                                    ? nhentaiHtml.All(index)
                                    : nhentaiHtml.Search(query, index);

                                HtmlNode root;

                                using (var response = await _http.GetAsync(url, false, token))
                                using (var reader = new StringReader(await response.Content.ReadAsStringAsync()))
                                {
                                    var doc = new HtmlDocument();
                                    doc.Load(reader);

                                    root = doc.DocumentNode;
                                }

                                current = root
                                    .SelectNodes(nhentaiHtml.XPath.SearchItem)
                                    ?.Select(n => _galleryRegex.Match(n.Attributes["href"].Value).Value)
                                    .ToArray();

                                index++;

                                _logger.LogDebug($"Got page {index}: {current?.Length ?? 0} items");

                                return !Array.IsNullOrEmpty(current);
                            }
                            catch (Exception)
                            {
                                return false;
                            }
                        },
                        () => current,
                        () => { }
                    );
                })
                .SelectMany(list => AsyncEnumerable.CreateEnumerable(() =>
                {
                    var index = 0;
                    IDoujin current = null;

                    return AsyncEnumerable.CreateEnumerator(
                        async token =>
                        {
                            if (index == list.Length)
                                return false;

                            current = await GetAsync(list[index++], token);
                            return current != null;
                        },
                        () => current,
                        () => { }
                    );
                }))
                .AsCompletedTask();

        public override string ToString() => Name;

        public void Dispose()
        {
        }
    }
}
