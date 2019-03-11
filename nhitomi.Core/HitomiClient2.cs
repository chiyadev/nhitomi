// Copyright (c) 2018-2019 phosphene47
//
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace nhitomi.Core
{
    public static class Hitomi2
    {
    }

    /// <summary>
    /// Refer to https://namu.wiki/w/Hitomi.la#s-4.4.1 (korean)
    /// </summary>
    public class HitomiClient2 : IDoujinClient
    {
        public string Name { get; }
        public string Url { get; }
        public string IconUrl { get; }
        public DoujinClientMethod Method { get; }
        public Regex GalleryRegex { get; }
        public Task<IDoujin> GetAsync(string id) => throw new System.NotImplementedException();

        public Task<IAsyncEnumerable<IDoujin>> SearchAsync(string query) => throw new System.NotImplementedException();

        public Task UpdateAsync() => throw new System.NotImplementedException();

        public void Dispose()
        {
        }
    }
}
