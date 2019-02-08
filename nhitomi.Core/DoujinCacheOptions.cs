// Copyright (c) 2018-2019 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using Microsoft.Extensions.Caching.Memory;

namespace nhitomi
{
    public class DoujinCacheOptions : MemoryCacheEntryOptions
    {
        public static readonly TimeSpan Expiration = TimeSpan.FromMinutes(10);

        public DoujinCacheOptions()
        {
            AbsoluteExpirationRelativeToNow = Expiration;
        }
    }
}