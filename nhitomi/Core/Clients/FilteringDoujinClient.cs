// Copyright (c) 2018-2019 chiya.dev
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace nhitomi.Core.Clients
{
    /// <summary>
    /// This doujin client is used to filter out various illegal doujins such as lolicon and shotacon.
    /// </summary>
    public class FilteringDoujinClient : IDoujinClient
    {
        readonly IDoujinClient _impl;

        public FilteringDoujinClient(IDoujinClient impl)
        {
            _impl = impl;
        }

        public string Name => _impl.Name;
        public string Url => _impl.Url;
        public string IconUrl => _impl.IconUrl;

        public double RequestThrottle => _impl.RequestThrottle;

        public DoujinClientMethod Method => _impl.Method;

        public Regex GalleryRegex => _impl.GalleryRegex;

        public async Task<IDoujin> GetAsync(string id, CancellationToken cancellationToken = default) =>
            Filter(await _impl.GetAsync(id, cancellationToken));

        const int _maxConsecutiveFilters = 6;

        public async Task<IAsyncEnumerable<IDoujin>> SearchAsync(
            string query,
            CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrEmpty(query) &&
                _bannedKeywords.Any(query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(k => k.ToLowerInvariant()).Contains))
                return AsyncEnumerable.Empty<IDoujin>();

            var results = await _impl.SearchAsync(query, cancellationToken);

            return AsyncEnumerable.CreateEnumerable(() =>
            {
                var enumerator = results.GetEnumerator();

                return AsyncEnumerable.CreateEnumerator(
                    async token =>
                    {
                        for (var count = 0; count < _maxConsecutiveFilters && await enumerator.MoveNext(token);)
                        {
                            var filtered = Filter(enumerator.Current);

                            if (filtered == null)
                                count++;
                            else
                                return true;
                        }

                        return false;
                    },
                    () => enumerator.Current,
                    enumerator.Dispose
                );
            });
        }

        static readonly string[] _bannedKeywords = new[]
        {
            // Discord Community Guidelines: NO LOLICON OR SHOTACON
            "loli",
            "lolis",
            "lolicon",
            "lolicons",
            "shota",
            "shotas",
            "shotacon",
            "shotacons",
            "child",
            "children",
            "minor",
            "minors"
        };

        static IDoujin Filter(IDoujin doujin)
        {
            if (doujin?.Tags == null ||
                _bannedKeywords.Any(doujin.Tags.Contains))
                return null;

            return doujin;
        }

        public override string ToString() => $"{nameof(FilteringDoujinClient)} ({_impl})";

        public void Dispose() => _impl.Dispose();
    }

    public static class FilteringDoujinClientExtensions
    {
        public static IDoujinClient Filtered(this IDoujinClient client) => new FilteringDoujinClient(client);
    }
}