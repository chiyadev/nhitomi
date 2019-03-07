// Copyright (c) 2019 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using nhitomi.Core;

namespace nhitomi
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
        public DoujinClientMethod Method => _impl.Method;

        public Regex GalleryRegex => _impl.GalleryRegex;

        public async Task<IDoujin> GetAsync(string id) => filter(await _impl.GetAsync(id));
        public Task<Stream> GetStreamAsync(string url) => _impl.GetStreamAsync(url);

        public const int MaxConsecutiveFilters = 6;

        public async Task<IAsyncEnumerable<IDoujin>> SearchAsync(string query)
        {
            if (!string.IsNullOrEmpty(query) &&
                bannedKeywords.Any(query.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(k => k.ToLowerInvariant()).Contains))
                return AsyncEnumerable.Empty<IDoujin>();

            var results = await _impl.SearchAsync(query);

            return AsyncEnumerable.CreateEnumerable(() =>
            {
                var enumerator = results.GetEnumerator();

                return AsyncEnumerable.CreateEnumerator(
                    moveNext: async token =>
                    {
                        for (var count = 0; count < MaxConsecutiveFilters && await enumerator.MoveNext(token);)
                        {
                            var filtered = filter(enumerator.Current);

                            if (filtered == null)
                                count++;
                            else
                                return true;
                        }

                        return false;
                    },
                    current: () => enumerator.Current,
                    dispose: enumerator.Dispose
                );
            });
        }

        static string[] bannedKeywords = new[]
        {
            // Discord Community Guideline: NO LOLICON OR SHOTACON
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

        IDoujin filter(IDoujin doujin)
        {
            if (doujin?.Tags == null ||
                bannedKeywords.Any(doujin.Tags.Contains))
                return null;

            return doujin;
        }

        public Task UpdateAsync() => _impl.UpdateAsync();

        public void Dispose() => _impl.Dispose();
    }

    public static class FilteringDoujinClientExtensions
    {
        public static IDoujinClient Filtered(this IDoujinClient client) => new FilteringDoujinClient(client);
    }
}
