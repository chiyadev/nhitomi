// Copyright (c) 2018-2019 fate/loli
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using nhitomi.Core.Doujins;
using Newtonsoft.Json;

namespace nhitomi.Core.Clients
{
    public static class Hitomi
    {
        public const string GalleryRegex =
            @"\b((http|https):\/\/)?hitomi(\.la)?\/(galleries\/)?(?<Hitomi>[0-9]{1,7})\b";

        public static string Gallery(int id) => $"https://hitomi.la/galleries/{id}.html";

        public static string GalleryInfo(int id, char? server = null) =>
            $"https://{server}tn.hitomi.la/galleries/{id}.js";

        static char GetCdn(int id) => (char) ('a' + (id % 10 == 1 ? 0 : id) % 2);

        public static string Image(int id, string name) => $"https://{GetCdn(id)}a.hitomi.la/galleries/{id}/{name}";

        public static class XPath
        {
            const string _gallery = "//div[contains(@class,'gallery')]";
            const string _galleryInfo = "//div[contains(@class,'gallery-info')]";
            const string _galleryContent = "//div[contains(@class,'gallery-content')]";

            public const string Name = _gallery + "//a[contains(@href,'/reader/')]";
            public const string Artists = _gallery + "//a[contains(@href,'/artist/')]";
            public const string Groups = _gallery + "//a[contains(@href,'/group/')]";
            public const string Type = _gallery + "//a[contains(@href,'/type/')]";
            public const string Language = _galleryInfo + "//tr[3]//a";
            public const string Series = _gallery + "//a[contains(@href,'/series/')]";
            public const string Tags = _gallery + "//a[contains(@href,'/tag/')]";
            public const string Characters = _gallery + "//a[contains(@href,'/character/')]";
            public const string Date = _gallery + "//span[contains(@class,'date')]";

            public const string Item = _galleryContent + "//a[contains(@href,'/galleries/')]";
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

        public const string NozomiIndex = "https://ltn.hitomi.la/index-all.nozomi";
    }

    public sealed class HitomiClient : IDoujinClient
    {
        public string Name => nameof(Hitomi);
        public string Url => "https://hitomi.la/";
        public string IconUrl => "https://ltn.hitomi.la/favicon-160x160.png";

        public double RequestThrottle => 500;

        public DoujinClientMethod Method => DoujinClientMethod.Api;

        public Regex GalleryRegex { get; } =
            new Regex(Hitomi.GalleryRegex, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        readonly IHttpProxyClient _http;
        readonly JsonSerializer _json;
        readonly PhysicalCache _cache;
        readonly ILogger<HitomiClient> _logger;

        readonly Task _indexUpdateTask;
        readonly CancellationTokenSource _indexUpdateTokenSource;
        readonly SemaphoreSlim _indexSemaphore = new SemaphoreSlim(1);

        public HitomiClient(
            IHttpProxyClient http,
            JsonSerializer json,
            ILogger<HitomiClient> logger)
        {
            _http = http;
            _json = json;
            _cache = new PhysicalCache(Name, json);
            _logger = logger;

            _indexUpdateTokenSource = new CancellationTokenSource();
            _indexUpdateTask = UpdateIndicesAsync(_indexUpdateTokenSource.Token);
        }

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
                : new HitomiDoujin(this, data);
        }

        async Task<Hitomi.DoujinData> GetAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                HtmlNode root;

                using (var response = await _http.GetAsync(Hitomi.Gallery(id), true, cancellationToken))
                using (var reader = new StringReader(await response.Content.ReadAsStringAsync()))
                {
                    var doc = new HtmlDocument();
                    doc.Load(reader);

                    root = doc.DocumentNode;
                }

                // Filter out anime
                var type = InnerSanitized(root.SelectSingleNode(Hitomi.XPath.Type));
                if (type != null && type.Equals("anime", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation($"Skipping {id} because it is of type 'anime'.");
                    return null;
                }

                // Scrape data from HTML using XPath
                var data = new Hitomi.DoujinData
                {
                    id = id,
                    name = InnerSanitized(root.SelectSingleNode(Hitomi.XPath.Name)),
                    artists = root.SelectNodes(Hitomi.XPath.Artists)?.Select(InnerSanitized).ToArray(),
                    groups = root.SelectNodes(Hitomi.XPath.Groups)?.Select(InnerSanitized).ToArray(),
                    language = InnerSanitized(root.SelectSingleNode(Hitomi.XPath.Language)),
                    series = InnerSanitized(root.SelectSingleNode(Hitomi.XPath.Series)),
                    tags = root.SelectNodes(Hitomi.XPath.Tags)
                        ?.Select(n => Hitomi.DoujinData.Tag.Parse(InnerSanitized(n))).ToArray(),
                    characters = root.SelectNodes(Hitomi.XPath.Characters)?.Select(InnerSanitized).ToArray(),
                    date = InnerSanitized(root.SelectSingleNode(Hitomi.XPath.Date))
                };

                // Parse images
                using (var response = await _http.GetAsync(Hitomi.GalleryInfo(id), true, cancellationToken))
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

        static string InnerSanitized(HtmlNode node) =>
            node == null ? null : HtmlEntity.DeEntitize(node.InnerText).Trim();

        async Task<int> GetGalleryIndexVersionAsync(CancellationToken cancellationToken = default)
        {
            using (var response = await _http.GetAsync(Hitomi.GalleryIndexVersion, false, cancellationToken))
                return int.Parse(await response.Content.ReadAsStringAsync());
        }

        struct NodeData
        {
            public readonly ulong Offset;
            public readonly int Length;

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
            public readonly ulong[] SubnodeAddresses = new ulong[Hitomi.B + 1];
        }

        static IndexNode DecodeNode(BinaryReader reader)
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

            for (var i = 0; i < node.SubnodeAddresses.Length; i++)
            {
                var subnodeAddress = reader.ReadUInt64Be();

                node.SubnodeAddresses[i] = subnodeAddress;
            }

            return node;
        }

        async Task<IndexNode> GetGalleryNodeAtAddress(ulong address, CancellationToken cancellationToken = default)
        {
            var url = Hitomi.GalleryIndex(_galleryVersion);

            using (var memory = new MemoryStream())
            {
                using (var stream = await GetUrlAtRange(
                    url, address, address + Hitomi.MaxNodeSize - 1, cancellationToken))
                    await stream.CopyToAsync(memory, 4096, cancellationToken);

                memory.Position = 0;

                using (var reader = new BinaryReader(memory))
                {
                    var node = DecodeNode(reader);

                    _logger.LogDebug($"Decoded node at address {address}, " +
                                     $"{node.Keys.Count} keys, {node.SubnodeAddresses.Length} subnodes");

                    return node;
                }
            }
        }

        async Task<Stream> GetUrlAtRange(string url, ulong start, ulong end,
            CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            unchecked
            {
                request.Headers.Range = new RangeHeaderValue((long) start, (long) end);
            }

            var response = await _http.Client.SendAsync(request, cancellationToken);
            return await response.Content.ReadAsStreamAsync();
        }

        async Task<NodeData?> B_searchAsync(byte[] key, IndexNode node, CancellationToken cancellationToken = default)
        {
            try
            {
                // todo: could be much more optimized
                int compareArrayBuffers(byte[] dv1, byte[] dv2)
                {
                    /*let compare_arraybuffers = function(dv1, dv2) {
                            const top = Math.min(dv1.byteLength, dv2.byteLength);
                            for (let i = 0; i < top; i++) {
                                    if (dv1[i] < dv2[i]) {
                                            return -1;
                                    } else if (dv1[i] > dv2[i]) {
                                            return 1;
                                    }
                            }
                            return 0;
                    };*/
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
                    /*let locate_key = function(key, node) {
                            let cmp_result = -1;
                            let i;
                            for (i = 0; i < node.keys.length; i++) {
                                    cmp_result = compare_arraybuffers(key, node.keys[i]);
                                    if (cmp_result <= 0) {
                                            break;
                                    }
                            }
                            return [!cmp_result, i];
                    };*/
                    var cmpResult = -1;

                    for (i = 0; i < node.Keys.Count; i++)
                    {
                        cmpResult = compareArrayBuffers(key, node.Keys[i]);

                        if (cmpResult <= 0)
                            break;
                    }

                    return cmpResult == 0;
                }

                //special case for empty root
                if (node.Keys.Count == 0)
                    return null;

                if (locateKey(out var index))
                    return node.Data[index];

                //isLeaf
                if (node.SubnodeAddresses.All(address => address == 0))
                    return null;

                //it's in a subnode
                var subnode = await GetGalleryNodeAtAddress(node.SubnodeAddresses[index], cancellationToken);

                return await B_searchAsync(key, subnode, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Exception in {nameof(B_searchAsync)}, key {Encoding.Default.GetString(key)}");

                return null;
            }
        }

        async Task<List<int>> GetGalleryIdsFromData(NodeData data, CancellationToken cancellationToken = default)
        {
            var url = Hitomi.GalleryData(_galleryVersion);

            if (data.Length > 100000000 || data.Length <= 0)
                throw new Exception($"length {data.Length} is too long");

            using (var memory = new MemoryStream())
            {
                using (var stream = await GetUrlAtRange(
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

        readonly SHA256 _sha256 = SHA256.Create();

        byte[] HashTerm(string query)
        {
            var buffer = new byte[4];
            System.Array.Copy(_sha256.ComputeHash(Encoding.UTF8.GetBytes(query)), buffer, 4);
            return buffer;
        }

        public async Task<IAsyncEnumerable<IDoujin>> SearchAsync(
            string query,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<int> galleryIds;

            await _indexSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    galleryIds = _nozomiIndex;
                }
                else
                {
                    var node = await GetGalleryNodeAtAddress(0, cancellationToken);
                    var data = await B_searchAsync(HashTerm(query), node, cancellationToken);

                    if (data == null)
                        return AsyncEnumerable.Empty<IDoujin>();

                    galleryIds = await GetGalleryIdsFromData(data.Value, cancellationToken);
                }
            }
            finally
            {
                _indexSemaphore.Release();
            }

            return AsyncEnumerable.CreateEnumerable(() =>
            {
                var enumerator = galleryIds.GetEnumerator();
                Hitomi.DoujinData current = null;

                return AsyncEnumerable.CreateEnumerator(
                    async token =>
                    {
                        if (!enumerator.MoveNext())
                            return false;

                        current = await GetAsync(enumerator.Current, token);
                        return current != null;
                    },
                    () => new HitomiDoujin(this, current),
                    enumerator.Dispose);
            });
        }

        async Task<int[]> ReadNozomiIndexAsync(CancellationToken cancellationToken = default)
        {
            const string url = Hitomi.NozomiIndex;

            using (var memory = new MemoryStream())
            {
                using (var response = await _http.GetAsync(url, false, cancellationToken))
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

        long _galleryVersion;

        int[] _nozomiIndex = new int[0];

        async Task UpdateIndicesAsync(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await _indexSemaphore.WaitAsync(cancellationToken);
                try
                {
                    // update gallery index version
                    _galleryVersion = await GetGalleryIndexVersionAsync(cancellationToken);

                    _logger.LogDebug($"Updated gallery index version: {_galleryVersion}");

                    // update nozomi indices, used for listing (not searching)
                    _nozomiIndex = await ReadNozomiIndexAsync(cancellationToken);

                    _logger.LogDebug($"Updated Nozomi index: {_nozomiIndex.Length} items");
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Exception while updating gallery version or Nozomi index.");
                }
                finally
                {
                    _indexSemaphore.Release();
                }

                await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
            }
        }

        public override string ToString() => Name;

        public void Dispose()
        {
            _sha256.Dispose();

            _indexUpdateTokenSource.Cancel();
            _indexUpdateTask.Wait();

            _indexUpdateTask.Dispose();
            _indexUpdateTokenSource.Dispose();
        }
    }
}