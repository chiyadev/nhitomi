using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace nhitomi.Core.Clients.nhentai
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
                [JsonProperty("pages")] public ImageData[] Pages;

                public struct ImageData
                {
                    [JsonProperty("t")] public char T;
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
        readonly JsonSerializer _serializer;
        readonly ILogger<nhentaiClient> _logger;

        public nhentaiClient(IHttpClient http, JsonSerializer serializer, ILogger<nhentaiClient> logger)
        {
            _http = http;
            _serializer = serializer;
            _logger = logger;
        }

        public static string GetGalleryUrl(Doujin doujin) => $"https://nhentai.net/g/{doujin.SourceId}/";

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
            {
                if (!response.IsSuccessStatusCode)
                    return null;

                using (var textReader = new StringReader(await response.Content.ReadAsStringAsync()))
                using (var jsonReader = new JsonTextReader(textReader))
                    data = _serializer.Deserialize<nhentai.DoujinData>(jsonReader);
            }

            return new DoujinInfo
            {
                PrettyName = FixTitle(data.Title.Pretty),
                OriginalName = FixTitle(data.Title.Japanese),

                UploadTime = DateTimeOffset.FromUnixTimeSeconds(data.UploadDate).UtcDateTime,

                Source = this,
                SourceId = id,

                Artist = data.Tags?.FirstOrDefault(t => t.Type == "artist").Name,
                Group = data.Tags?.FirstOrDefault(t => t.Type == "group").Name,
                Scanlator = string.IsNullOrWhiteSpace(data.Scanlator) ? null : data.Scanlator,
                Language = data.Tags?.FirstOrDefault(t => t.Type == "language" && t.Name != "translated").Name,
                Parody = data.Tags?.FirstOrDefault(t => t.Type == "parody" && t.Name != "original").Name,

                Characters = data.Tags?.Where(t => t.Type == "character").Select(t => t.Name),
                Categories = data.Tags?.Where(t => t.Type == "category" && t.Name != "doujinshi").Select(t => t.Name),
                Tags = data.Tags?.Where(t => t.Type == "tag").Select(t => t.Name),

                Data = _serializer.Serialize(new InternalDoujinData
                {
                    MediaId = data.MediaId,
                    Extensions = new string(data.Images.Pages.Select(p => p.T).ToArray())
                }),
                PageCount = data.Images.Pages.Length
            };
        }

        // regex to match () and [] in titles
        static readonly Regex _bracketsRegex = new Regex(@"\([^)]*\)|\[[^\]]*\]",
            RegexOptions.Compiled | RegexOptions.Singleline);

        static string FixTitle(string japanese)
        {
            if (string.IsNullOrWhiteSpace(japanese))
                return null;

            // replace stuff in brackets with nothing
            japanese = _bracketsRegex.Replace(japanese, "").Trim();

            return string.IsNullOrEmpty(japanese) ? null : HttpUtility.HtmlDecode(japanese);
        }

        sealed class InternalDoujinData
        {
            [JsonProperty("i")] public int MediaId;
            [JsonProperty("e")] public string Extensions;
        }

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
            {
                if (!response.IsSuccessStatusCode)
                    return null;

                using (var textReader = new StringReader(await response.Content.ReadAsStringAsync()))
                using (var jsonReader = new JsonTextReader(textReader))
                {
                    latestId = _serializer.Deserialize<nhentai.ListData>(jsonReader).Results
                        .OrderByDescending(d => d.Id)
                        .First().Id;
                }
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

        public IEnumerable<string> PopulatePages(Doujin doujin)
        {
            var data = _serializer.Deserialize<InternalDoujinData>(doujin.Data);

            if (data.MediaId == 0 || data.Extensions == null)
                yield break;

            for (var i = 0; i < data.Extensions.Length; i++)
            {
                var ext = data.Extensions[i];

                yield return nhentai.Image(data.MediaId, i, FixExtension(ext));
            }
        }

        public void InitializeImageRequest(Doujin doujin, HttpRequestMessage message)
        {
        }

        static string FixExtension(char ext) => ext == 'p' ? "png" : "jpg";

        public void Dispose()
        {
        }
    }
}