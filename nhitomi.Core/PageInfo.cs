// Copyright (c) 2019 phosphene47
//
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

namespace nhitomi.Core
{
    public class PageInfo
    {
        public int Index { get; }
        public string Extension { get; }
        public string Url { get; }

        public PageInfo(int index, string extension, string url)
        {
            Index = index;
            Extension = extension;
            Url = url;
        }
    }
}
