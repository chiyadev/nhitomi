// Copyright (c) 2018-2019 chiya.dev
//
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using Newtonsoft.Json;

namespace nhitomi.Core
{
    public sealed class PageInfo
    {
        [JsonProperty("i")] public int Index { get; }
        [JsonProperty("e")] public string Extension { get; }
        [JsonProperty("u")] public string Url { get; }

        public PageInfo(int index, string extension, string url)
        {
            Index = index;
            Extension = extension;
            Url = url;
        }
    }
}
