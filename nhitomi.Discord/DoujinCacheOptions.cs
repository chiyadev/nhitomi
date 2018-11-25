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