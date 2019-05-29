using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace nhitomi.Core.Clients
{
    public static class nhentai
    {
        public static string Gallery(int id) => $"https://nhentai.net/api/gallery/{id}";
        public static string All(int index = 0) => $"https://nhentai.net/api/galleries/all?page={index + 1}";

        public static string Search(string query, int index = 0) =>
            $"https://nhentai.net/api/galleries/search?query={query}&page={index + 1}";

        public static string Image(int mediaId, int index, string ext) =>
            $"https://i.nhentai.net/galleries/{mediaId}/{index + 1}.{ext}";

        public static string ThumbImage(int mediaId, int index, string ext) =>
            $"https://t.nhentai.net/galleries/{mediaId}/{index + 1}t.{ext}";

        public sealed class DoujinData
        {
            [JsonProperty("id")] public int Id;
            [JsonProperty("media_id")] public int MediaId;
            [JsonProperty("scanlator")] public string Scanlator;
            [JsonProperty("upload_date")] public long UploadDate;

            [JsonProperty("title")] public TitleData Title;

            public struct TitleData
            {
                [JsonProperty("japanese")] public string Japanese;
                [JsonProperty("pretty")] public string Pretty;
            }

            [JsonProperty("images")] public ImagesData Images;

            public struct ImagesData
            {
                [JsonProperty("images")] public ImageData[] Pages;

                public struct ImageData
                {
                    [JsonProperty("t")] public string T;
                }
            }

            [JsonProperty("tags")] public TagData[] Tags;

            public struct TagData
            {
                [JsonProperty("type")] public string Type;
                [JsonProperty("name")] public string Name;
            }
        }

        public sealed class ListData
        {
            [JsonProperty("result")] public DoujinData[] Results;

            [JsonProperty("num_pages")] public int NumPages;
            [JsonProperty("per_page")] public int PerPage;
        }
    }

    public sealed class nhentaiClient : IDoujinClient
    {
        public string Name => nameof(nhentai);
        public string Url => "https://nhentai.net/";

        readonly IHttpClient _http;
        readonly JsonSerializer _json;
        readonly ILogger<nhentaiClient> _logger;

        public nhentaiClient(IHttpClient http, JsonSerializer json, ILogger<nhentaiClient> logger)
        {
            _http = http;
            _json = json;
            _logger = logger;
        }

        public async Task<DoujinInfo> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            if (!int.TryParse(id, out var intId))
                return null;

            nhentai.DoujinData data;

            using (var response = await _http.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(nhentai.Gallery(intId))
            }, cancellationToken))
            using (var textReader = new StringReader(await response.Content.ReadAsStringAsync()))
            using (var jsonReader = new JsonTextReader(textReader))
                data = _json.Deserialize<nhentai.DoujinData>(jsonReader);

            return new DoujinInfo
            {
                GalleryUrl = $"https://nhentai.net/g/{id}/",

                PrettyName = data.Title.Pretty,
                OriginalName = data.Title.Japanese,

                UploadTime = DateTimeOffset.FromUnixTimeSeconds(data.UploadDate).UtcDateTime,

                Source = this,
                SourceId = id,

                Artist = data.Tags?.FirstOrDefault(t => t.Type == "artist").Name,
                Scanlator = data.Scanlator,
                Language = data.Tags?.FirstOrDefault(t => t.Type == "language" && t.Name != "translated").Name,
                ParodyOf = data.Tags?.FirstOrDefault(t => t.Type == "parody" && t.Name != "original").Name,

                Characters = data.Tags?.Where(t => t.Type == "character").Select(t => t.Name),
                Categories = data.Tags?.Where(t => t.Type == "category" && t.Name != "doujinshi").Select(t => t.Name),
                Tags = data.Tags?.Where(t => t.Type == "tag").Select(t => t.Name),

                Images = data.Images.Pages?.Select(p =>
                    nhentai.Image(data.MediaId, Array.IndexOf(data.Images.Pages, p), FixExtension(p.T)))
            };
        }

        static string FixExtension(string ext) => ext[0] == 'p' ? "png" : "jpg";

        public async Task<IEnumerable<string>> EnumerateAsync(string startId = null,
            CancellationToken cancellationToken = default)
        {
            int latestId;

            // get the latest doujin id
            using (var response = await _http.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(nhentai.All(0))
            }, cancellationToken))
            using (var textReader = new StringReader(await response.Content.ReadAsStringAsync()))
            using (var jsonReader = new JsonTextReader(textReader))
            {
                latestId = _json.Deserialize<nhentai.ListData>(jsonReader).Results
                    .OrderByDescending(d => d.Id)
                    .First().Id;
            }

            int.TryParse(startId, out var oldestId);

            return EnumerateIds(oldestId, latestId);
        }

        static IEnumerable<string> EnumerateIds(int oldest, int latest)
        {
            // assume all doujins are available
            for (var i = oldest; i <= latest; i++)
                yield return i.ToString();
        }

        public void Dispose()
        {
        }
    }
}