// Copyright (c) 2018 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

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
    public static class nhentai
    {
        public const int RequestCooldown = 500;

        public const string GalleryRegex = @"\b((http|https):\/\/)?nhentai(\.net)?\/(g\/)?(?<nhentai>[0-9]{1,6})\b";

        public static string Gallery(int id) => $"https://nhentai.net/api/gallery/{id}";
        public static string All(int index = 0) => $"https://nhentai.net/api/galleries/all?page={index + 1}";
        public static string Search(string query, int index = 0) => $"https://nhentai.net/api/galleries/search?query={query}&page={index + 1}";

        public static string Image(int mediaId, int index, string ext) => $"https://i.nhentai.net/galleries/{mediaId}/{index + 1}.{ext}";
        public static string ThumbImage(int mediaId, int index, string ext) => $"https://t.nhentai.net/galleries/{mediaId}/{index + 1}t.{ext}";

        public sealed class DoujinData
        {
            public readonly DateTime _processed = DateTime.UtcNow;

            public int id;
            public int media_id;
            public string scanlator;
            public long upload_date;

            public Title title;
            public struct Title
            {
                public string japanese;
                public string pretty;
            }

            public Images images;
            public struct Images
            {
                public Image[] pages;
                public struct Image
                {
                    public string t;
                }
            }

            public Tag[] tags;
            public struct Tag
            {
                public string type;
                public string name;
            }
        }

        public sealed class ListData
        {
            public readonly DateTime _processed = DateTime.UtcNow;

            public DoujinData[] result;

            public int num_pages;
            public int per_page;
        }
    }

    /// <summary>
    /// Legacy nhentai client using the HTTP API endpoints which have been suspended indefinitely as of January 13th 2019.
    /// https://twitter.com/fuckmaou/status/1084550608097603585
    /// https://github.com/NHMoeDev/NHentai-android/issues/108
    /// </summary>
    [Obsolete]
    public sealed class nhentaiClient : IDoujinClient
    {
        public string Name => nameof(nhentai);
        public string Url => "https://nhentai.net/";
        public string IconUrl => "https://cdn.cybrhome.com/media/website/live/icon/icon_nhentai.net_57f740.png";

        public DoujinClientMethod Method => DoujinClientMethod.Api;

        public Regex GalleryRegex { get; } = new Regex(nhentai.GalleryRegex, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        readonly IMemoryCache _cache;
        readonly HttpClient _http;
        readonly JsonSerializer _json;
        readonly ILogger _logger;

        public nhentaiClient(
            IHttpClientFactory httpFactory,
            IMemoryCache cache,
            JsonSerializer json,
            ILogger<nhentaiClient> logger
        )
        {
            _http = httpFactory?.CreateClient(Name);
            _cache = cache;
            _json = json;
            _logger = logger;
        }

        IDoujin wrap(nhentai.DoujinData data) => new nhentaiDoujin(this, data);

        Task throttle() => Task.Delay(TimeSpan.FromMilliseconds(nhentai.RequestCooldown));

        public async Task<IDoujin> GetAsync(string id)
        {
            if (!int.TryParse(id, out var intId))
                return null;

            return wrap(
                await _cache.GetOrCreateAsync<nhentai.DoujinData>(
                    key: $"{Name}/{id}",
                    factory: entry =>
                    {
                        entry.AbsoluteExpirationRelativeToNow = DoujinCacheOptions.Expiration;
                        return getAsync();
                    }
                )
            );

            async Task<nhentai.DoujinData> getAsync()
            {
                try
                {
                    nhentai.DoujinData data;

                    using (var response = await _http.GetAsync(nhentai.Gallery(intId)))
                    using (var textReader = new StringReader(await response.Content.ReadAsStringAsync()))
                    using (var jsonReader = new JsonTextReader(textReader))
                        data = _json.Deserialize<nhentai.DoujinData>(jsonReader);

                    _logger.LogDebug($"Got doujin {id}: {data.title.pretty}");

                    return data;
                }
                catch (Exception) { return null; }
                finally
                {
                    await throttle();
                }
            }
        }

        public Task<IAsyncEnumerable<IDoujin>> SearchAsync(string query) =>
            AsyncEnumerable.CreateEnumerable(() =>
            {
                nhentai.ListData current = null;
                var index = 0;

                return AsyncEnumerable.CreateEnumerator(
                    moveNext: async token =>
                    {
                        try
                        {
                            // Load list
                            var url = string.IsNullOrWhiteSpace(query)
                                ? nhentai.All(index)
                                : nhentai.Search(query, index);

                            using (var response = await _http.GetAsync(url))
                            using (var textReader = new StringReader(await response.Content.ReadAsStringAsync()))
                            using (var jsonReader = new JsonTextReader(textReader))
                                current = _json.Deserialize<nhentai.ListData>(jsonReader);

                            // Add results to cache
                            foreach (var result in current.result)
                                _cache.Set(
                                    key: $"{Name}/{result.id}",
                                    value: result,
                                    options: new DoujinCacheOptions()
                                );

                            index++;

                            _logger.LogDebug($"Got page {index}: {current.result?.Length ?? 0} items");

                            return !Array.IsNullOrEmpty(current.result);
                        }
                        catch (Exception) { return false; }
                        finally
                        {
                            await throttle();
                        }
                    },
                    current: () => current,
                    dispose: () => { }
                );
            })
            .SelectMany(l => l.result.Select(wrap).ToAsyncEnumerable())
            .AsCompletedTask();

        public Task UpdateAsync() => Task.CompletedTask;

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

        public override string ToString() => Name;

        public void Dispose() { }
    }
}
