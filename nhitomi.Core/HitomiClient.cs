// Copyright (c) 2018-2019 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace nhitomi.Core
{
    public static class Hitomi
    {
        public const int RequestCooldown = 500;

        public const string GalleryRegex =
            @"\b((http|https):\/\/)?hitomi(\.la)?\/(galleries\/)?(?<Hitomi>[0-9]{1,7})\b";

        public static string Gallery(int id) => $"https://hitomi.la/galleries/{id}.html";

        public static string GalleryInfo(int id, char? server = null) =>
            $"https://{server}tn.hitomi.la/galleries/{id}.js";

        public static char GetCdn(int id) => (char) ('a' + (id % 10 == 1 ? 0 : id) % 2);

        public static string Image(int id, string name) => $"https://{GetCdn(id)}a.hitomi.la/galleries/{id}/{name}";

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

        public sealed class DoujinData
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
                        ? str[0] == 'm' ? '♀'
                        : str[0] == 'f' ? '♂'
                        : (char?) null
                        : str.EndsWith("♀")
                            ? '♀'
                            : str.EndsWith("♂")
                                ? '♂'
                                : (char?) null
                };

                public string Value;
                public char? Sex;
            }
        }

        public const int B = 16;
        public const int MaxNodeSize = 464;

        static long UnixTimestamp => ((DateTimeOffset) DateTime.UtcNow).ToUnixTimeSeconds();

        public static string GalleryIndexVersion => $"https://ltn.hitomi.la/galleriesindex/version?_={UnixTimestamp}";

        public static string GalleryIndex(long version) =>
            $"https://ltn.hitomi.la/galleriesindex/galleries.{version}.index";

        public static string GalleryData(long version) =>
            $"https://ltn.hitomi.la/galleriesindex/galleries.{version}.data";
    }

    public sealed class HitomiClient : IDoujinClient
    {
        public string Name => nameof(Hitomi);
        public string Url => "https://hitomi.la/";
        public string IconUrl => "https://ltn.hitomi.la/favicon-160x160.png";

        public DoujinClientMethod Method => DoujinClientMethod.Api;

        public Regex GalleryRegex { get; } =
            new Regex(Hitomi.GalleryRegex, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        readonly PhysicalCache _cache;
        readonly HttpClient _http;
        readonly JsonSerializer _json;
        readonly ILogger _logger;

        public HitomiClient(
            IHttpClientFactory httpFactory,
            JsonSerializer json,
            ILogger<HitomiClient> logger
        )
        {
            _http = httpFactory?.CreateClient(Name);
            _cache = new PhysicalCache(Name, json);
            _json = json;
            _logger = logger;
        }

        IDoujin wrap(Hitomi.DoujinData data) => data == null ? null : new HitomiDoujin(this, data);

        public async Task<IDoujin> GetAsync(string id)
        {
            if (!int.TryParse(id, out var intId))
                return null;

            return wrap(
                await _cache.GetOrCreateAsync(id, getAsync)
            );

            async Task<Hitomi.DoujinData> getAsync()
            {
                try
                {
                    HtmlNode root;

                    using (var response = await _http.GetAsync(Hitomi.Gallery(intId)))
                    using (var reader = new StringReader(await response.Content.ReadAsStringAsync()))
                    {
                        var doc = new HtmlDocument();
                        doc.Load(reader);

                        root = doc.DocumentNode;
                    }

                    // Filter out anime
                    var type = innerSanitized(root.SelectSingleNode(Hitomi.XPath.Type));
                    if (type != null && type.Equals("anime", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation($"Skipping {id} because it is of type 'anime'.");
                        return null;
                    }

                    // Scrape data from HTML using XPath
                    var data = new Hitomi.DoujinData
                    {
                        id = intId,
                        name = innerSanitized(root.SelectSingleNode(Hitomi.XPath.Name)),
                        artists = root.SelectNodes(Hitomi.XPath.Artists)?.Select(innerSanitized).ToArray(),
                        groups = root.SelectNodes(Hitomi.XPath.Groups)?.Select(innerSanitized).ToArray(),
                        language = innerSanitized(root.SelectSingleNode(Hitomi.XPath.Language)),
                        series = innerSanitized(root.SelectSingleNode(Hitomi.XPath.Series)),
                        tags = root.SelectNodes(Hitomi.XPath.Tags)
                            ?.Select(n => Hitomi.DoujinData.Tag.Parse(innerSanitized(n))).ToArray(),
                        characters = root.SelectNodes(Hitomi.XPath.Characters)?.Select(innerSanitized).ToArray(),
                        date = innerSanitized(root.SelectSingleNode(Hitomi.XPath.Date))
                    };

                    // Parse images
                    using (var response = await _http.GetAsync(Hitomi.GalleryInfo(intId)))
                    using (var textReader = new StringReader(await response.Content.ReadAsStringAsync()))
                    using (var jsonReader = new JsonTextReader(textReader))
                    {
                        // Discard javascript and start at actual json
                        while ((char) textReader.Peek() != '[')
                            textReader.Read();

                        data.images = _json.Deserialize<Hitomi.DoujinData.Image[]>(jsonReader);
                    }

                    _logger.LogDebug($"Got doujin {id}: {data.name}");

                    return data;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        static string innerSanitized(HtmlNode node) =>
            node == null ? null : HtmlEntity.DeEntitize(node.InnerText).Trim();

        async Task<int> getGalleryIndexVersionAsync() =>
            int.Parse(await _http.GetStringAsync(Hitomi.GalleryIndexVersion));

        struct NodeData
        {
            public ulong Offset;
            public int Length;

            public NodeData(ulong offset, int length)
            {
                Offset = offset;
                Length = length;
            }
        }

        sealed class IndexNode
        {
            public readonly List<byte[]> Keys = new List<byte[]>();
            public readonly List<NodeData> Data = new List<NodeData>();
            public readonly List<ulong> SubnodeAddresses = new List<ulong>();
        }

        static IndexNode decodeNode(BinaryReader reader)
        {
            var node = new IndexNode();

            var numberOfKeys = reader.ReadInt32Be();

            for (var i = 0; i < numberOfKeys; i++)
            {
                var keySize = reader.ReadInt32Be();

                if (keySize == 0 || keySize > 32)
                    throw new Exception("fatal: !key_size || key_size > 32");

                node.Keys.Add(reader.ReadBytes(keySize));
            }

            var numberOfData = reader.ReadInt32Be();

            for (var i = 0; i < numberOfData; i++)
            {
                var offset = reader.ReadUInt64Be();
                var length = reader.ReadInt32Be();

                node.Data.Add(new NodeData(offset, length));
            }

            const int numberOfSubnodeAddresses = Hitomi.B + 1;

            for (var i = 0; i < numberOfSubnodeAddresses; i++)
            {
                var subnodeAddress = reader.ReadUInt64Be();

                node.SubnodeAddresses.Add(subnodeAddress);
            }

            return node;
        }

        async Task<IndexNode> getGalleryNodeAtAddress(long version, ulong address,
            CancellationToken cancellationToken = default)
        {
            var url = Hitomi.GalleryIndex(version);

            using (var memory = new MemoryStream())
            {
                using (var stream = await getUrlAtRange(
                    url, address, address + Hitomi.MaxNodeSize - 1, cancellationToken))
                    await stream.CopyToAsync(memory, 4096, cancellationToken);

                memory.Position = 0;

                using (var reader = new BinaryReader(memory))
                    return decodeNode(reader);
            }
        }

        async Task<Stream> getUrlAtRange(string url, ulong start, ulong end,
            CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            unchecked
            {
                request.Headers.Range = new RangeHeaderValue((long) start, (long) end);
            }

            var response = await _http.SendAsync(request, cancellationToken);
            return await response.Content.ReadAsStreamAsync();
        }

        async Task<NodeData?> B_searchAsync(long version, byte[] key, IndexNode node,
            CancellationToken cancellationToken = default)
        {
            // todo: could be mucb more optimized
            int compareArrayBuffers(byte[] dv1, byte[] dv2)
            {
                var top = Math.Min(dv1.Length, dv2.Length);

                for (var i = 0; i < top; i++)
                {
                    if (dv1[i] < dv2[i])
                        return -1;
                    if (dv1[i] > dv2[i])
                        return 1;
                }

                return 0;
            }

            bool locateKey(out int i)
            {
                var cmpResult = -1;

                for (i = 0; i < node.Keys.Count; i++)
                {
                    cmpResult = compareArrayBuffers(key, node.Keys[i]);

                    if (cmpResult <= 0)
                        break;
                }

                return cmpResult != 0;
            }

            if (locateKey(out var index))
                return node.Data[index];

            if (node.SubnodeAddresses.Count == 0)
                return null;

            //it's in a subnode
            var subnode = await getGalleryNodeAtAddress(version, node.SubnodeAddresses[index], cancellationToken);

            return await B_searchAsync(version, key, subnode, cancellationToken);
        }

        async Task<List<int>> getGalleryIdsFromData(long version, NodeData data,
            CancellationToken cancellationToken = default)
        {
            var url = Hitomi.GalleryData(version);

            if (data.Length > 100000000 || data.Length <= 0)
                throw new Exception($"length {data.Length} is too long");

            using (var memory = new MemoryStream())
            {
                using (var stream = await getUrlAtRange(
                    url, data.Offset, data.Offset + (ulong) data.Length - 1, cancellationToken))
                    await stream.CopyToAsync(memory, 4096, cancellationToken);

                memory.Position = 0;

                using (var reader = new BinaryReader(memory))
                {
                    var galleryIds = new List<int>();
                    var numberOfGalleryIds = reader.ReadInt32Be();

                    var expectedLength = sizeof(int) + numberOfGalleryIds * sizeof(int);
                    if (memory.Length != expectedLength)
                        throw new Exception($"inbuf.byteLength {memory.Length} !== expected_length {expectedLength}");

                    for (var i = 0; i < numberOfGalleryIds; i++)
                        galleryIds.Add(reader.ReadInt32Be());

                    return galleryIds;
                }
            }
        }

        static byte[] hashTerm(string query) => null; //sha256 slice(0,4)

        public async Task<IAsyncEnumerable<IDoujin>> SearchAsync(string query)
        {
            var version = await getGalleryIndexVersionAsync();
            var data = await B_searchAsync(version, hashTerm(query), await getGalleryNodeAtAddress(version, 0));

            if (data == null)
                return AsyncEnumerable.Empty<IDoujin>();

            var galleryIds = await getGalleryIdsFromData(version, data.Value);

            return AsyncEnumerable.CreateEnumerable(() =>
            {
                var enumerator = galleryIds.GetEnumerator();
                IDoujin current = null;

                return AsyncEnumerable.CreateEnumerator(
                    moveNext: async token =>
                    {
                        if (!enumerator.MoveNext())
                            return false;

                        current = await GetAsync(enumerator.Current.ToString());
                        return true;
                    },
                    () => current,
                    enumerator.Dispose);
            });
        }

        public Task UpdateAsync() => throw new NotImplementedException();

        public double RequestThrottle => Hitomi.RequestCooldown;

        public override string ToString() => Name;

        public void Dispose()
        {
        }
    }
}
