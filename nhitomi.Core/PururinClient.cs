// Copyright (c) 2018-2019 phosphene47
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
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace nhitomi
{
    public static class Pururin
    {
        public const int RequestCooldown = 500;

        public const string GalleryRegex = @"\b((http|https):\/\/)?pururin(\.io)?\/(gallery\/)?(?<pururin>[0-9]{1,5})(\/\S+)?\b";

        public const string Gallery = "https://pururin.io/api/contribute/gallery/info";
        public const string Search = "https://pururin.io/api/search/advance";

        public static string GalleryRequest(int id) =>
$@"{{
    ""id"": {id},
    ""type"": 2
}}";
        public static string SearchRequest(string query) =>
$@"{{
    ""search"": {{
        ""sort"": ""newest"",
        ""manga"": {{
            ""string"": ""{query}"",
            ""sort"": ""1""
        }}
    }}
}}";

        public static string Image(int galleryId, int index, string ext) => $"https://cdn.pururin.io/assets/images/data/{galleryId}/{index + 1}.{ext}";
        public static string ThumbImage(int galleryId, int index, string ext) => $"https://cdn.pururin.io/assets/images/data/{galleryId}/{index + 1}t.{ext}";

        public struct TagData
        {
            public int id;
            public string name;
            public string j_name;
            public int type;
            public string full_title;
            public string type_name;
            public string slug;
        }

        public sealed class DoujinData
        {
            public readonly DateTime _processed = DateTime.UtcNow;

            public bool status;

            public Gallery gallery;
            public struct Gallery
            {
                public int id;
                public int user_id;
                public string title;
                public string j_title;
                public string full_title;
                public string summary;
                public int total_pages;
                public string source;
                public double average_rating;
                public string slug;
                public string image_extension;
                public string clean_title;
                public string clean_japan_title;
                public string clean_full_title;

                public Dictionary<string, TagData[]> tags;
            }
        }

        public sealed class ListDataContainer
        {
            public bool status;
            public string results;
        }

        public sealed class ListData
        {
            public int current_page;

            public ListItem[] data;
            public struct ListItem
            {
                public int id;

                // would've appreciated having the same interface as DoujinData.Gallery
                // unfortunately search results don't have full tag info
            }

            public string first_page_url;
            public int last_page;
            public string last_page_url;
            public string next_page_url;
            public string path;
            public int per_page;
            public string prev_page_url;
            public int to;
            public int total;
        }
    }

    public class PururinClient : IDoujinClient
    {
        public string Name => nameof(Pururin);
        public string Url => "https://pururin.io/";
        public string IconUrl => "https://pururin.io/assets/images/logo.png";

        public DoujinClientMethod Method => DoujinClientMethod.Api;

        public Regex GalleryRegex => new Regex(Pururin.GalleryRegex, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        readonly PhysicalCache _cache;
        readonly HttpClient _http;
        readonly JsonSerializer _json;
        readonly ILogger _logger;

        public PururinClient(
            IHttpClientFactory httpFactory,
            JsonSerializer json,
            ILogger<PururinClient> logger
        )
        {
            _http = httpFactory?.CreateClient(Name);
            _cache = new PhysicalCache(Name, json);
            _json = json;
            _logger = logger;
        }

        IDoujin wrap(Pururin.DoujinData data) => data == null ? null : new PururinDoujin(this, data);

        Task throttle() => Task.Delay(TimeSpan.FromMilliseconds(Pururin.RequestCooldown));

        public async Task<IDoujin> GetAsync(string id)
        {
            if (!int.TryParse(id, out var intId))
                return null;

            return wrap(
                await _cache.GetOrCreateAsync<Pururin.DoujinData>(
                    name: id,
                    getAsync: getAsync
                )
            );

            async Task<Pururin.DoujinData> getAsync()
            {
                try
                {
                    Pururin.DoujinData data;

                    using (var response = await _http.PostAsync(Pururin.Gallery, new StringContent(Pururin.GalleryRequest(intId))))
                    using (var textReader = new StringReader(await response.Content.ReadAsStringAsync()))
                    using (var jsonReader = new JsonTextReader(textReader))
                        data = _json.Deserialize<Pururin.DoujinData>(jsonReader);

                    _logger.LogDebug($"Got doujin {id}: {data.gallery.clean_full_title}");

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
                Pururin.ListData current = null;
                var nextPage = Pururin.Search;

                return AsyncEnumerable.CreateEnumerator(
                    moveNext: async token =>
                    {
                        try
                        {
                            // Load list
                            using (var response = await _http.PostAsync(nextPage, new StringContent(Pururin.SearchRequest(query))))
                            using (var textReader = new StringReader(await response.Content.ReadAsStringAsync()))
                            using (var jsonReader = new JsonTextReader(textReader))
                                current = _json.Deserialize<Pururin.ListData>(jsonReader);

                            _logger.LogDebug($"Got page {current.current_page}: {current?.per_page ?? 0} items");

                            return (nextPage = current.next_page_url) != null;
                        }
                        catch (Exception) { return false; }
                        finally
                        {
                            await throttle();
                        }
                    },
                    current: () => current.data,
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
                        if (index == list.Length)
                            return false;

                        current = await GetAsync(list[index++].id.ToString());
                        return true;
                    },
                    current: () => current,
                    dispose: () => { }
                );
            }))
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