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
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace nhitomi.Core
{
    public static class Hitomi2
    {
        public const int B = 16;
        public const int MaxNodeSize = 464;

        static long UnixTimestamp => ((DateTimeOffset) DateTime.UtcNow).ToUnixTimeSeconds();

        public static string GalleryIndexVersion => $"https://ltn.hitomi.la/galleriesindex/version?_={UnixTimestamp}";

        public static string GalleryIndex(long version) =>
            $"https://ltn.hitomi.la/galleriesindex/galleries.{version}.index";

        public static string GalleryData(long version) =>
            $"https://ltn.hitomi.la/galleriesindex/galleries.{version}.data";
    }

    /// <summary>
    /// Refer to https://namu.wiki/w/Hitomi.la#s-4.4.1 (Korean)
    /// </summary>
    public class HitomiClient2 : IDoujinClient
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

        public HitomiClient2(
            IHttpClientFactory httpFactory,
            JsonSerializer json,
            ILogger<HitomiClient2> logger
        )
        {
            _http = httpFactory?.CreateClient(Name);
            _cache = new PhysicalCache(Name, json);
            _json = json;
            _logger = logger;
        }

        public Task<IDoujin> GetAsync(string id) => throw new NotImplementedException();

        async Task<int> getGalleryIndexVersionAsync() =>
            int.Parse(await _http.GetStringAsync(Hitomi2.GalleryIndexVersion));

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
            public readonly List<ulong> SubnodeAdresses = new List<ulong>();
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

            var numberOfSubnodeAddresses = Hitomi2.B + 1;

            for (var i = 0; i < numberOfSubnodeAddresses; i++)
            {
                var subnodeAddress = reader.ReadUInt64Be();

                node.SubnodeAdresses.Add(subnodeAddress);
            }

            return node;
        }

        async Task<IndexNode> getGalleryNodeAtAddress(long version, ulong address,
            CancellationToken cancellationToken = default)
        {
            var url = Hitomi2.GalleryIndex(version);

            using (var memory = new MemoryStream())
            {
                using (var stream = await getUrlAtRange(url, address, address + Hitomi2.MaxNodeSize, cancellationToken))
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

            if (node.SubnodeAdresses.Count == 0)
                return null;

            //it's in a subnode
            var subnode = await getGalleryNodeAtAddress(version, node.SubnodeAdresses[index], cancellationToken);

            return await B_searchAsync(version, key, subnode, cancellationToken);
        }

        async Task<List<int>> getGalleryIdsFromData(long version, NodeData data,
            CancellationToken cancellationToken = default)
        {
            var url = Hitomi2.GalleryData(version);

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

        public void Dispose()
        {
        }
    }
}
