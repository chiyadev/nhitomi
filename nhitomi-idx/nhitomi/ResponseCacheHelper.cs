using System;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Net.Http.Headers;

namespace nhitomi
{
    public enum CacheControlMode
    {
        Never = 0,
        Aggressive = 1,
        AllowWithRevalidate = 2
    }

    public static class ResponseCacheHelper
    {
        public static void SetCacheControl(this ResponseHeaders headers, CacheControlMode mode)
        {
            switch (mode)
            {
                case CacheControlMode.Never:
                    headers.CacheControl = new CacheControlHeaderValue
                    {
                        NoStore = true,
                        MaxAge  = TimeSpan.Zero
                    };
                    break;

                case CacheControlMode.Aggressive:
                    headers.CacheControl = new CacheControlHeaderValue
                    {
                        Public = true,
                        MaxAge = TimeSpan.FromSeconds(int.MaxValue),
                        Extensions =
                        {
                            new NameValueHeaderValue("immutable")
                        }
                    };
                    break;

                case CacheControlMode.AllowWithRevalidate:
                    headers.CacheControl = new CacheControlHeaderValue
                    {
                        Public  = true,
                        NoCache = true,
                        MaxAge  = TimeSpan.Zero
                    };
                    break;
            }
        }
    }
}