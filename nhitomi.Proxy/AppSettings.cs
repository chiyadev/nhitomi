// Copyright (c) 2018-2019 chiya.dev
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

namespace nhitomi.Proxy
{
    public sealed class AppSettings
    {
        public DiscordSettings Discord { get; set; } = new DiscordSettings();

        public sealed class DiscordSettings
        {
            public string Token { get; set; }
        }

        public HttpSettings Http { get; set; } = new HttpSettings();

        public sealed class HttpSettings
        {
            public string MainUrl { get; set; }
            public string Url { get; set; }
        }
    }
}