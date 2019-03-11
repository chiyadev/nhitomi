// Copyright (c) 2018-2019 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;

namespace nhitomi.Proxy
{
    public sealed class AppSettings
    {
        public DiscordSettings Discord { get; set; }

        public sealed class DiscordSettings
        {
            public string Token { get; set; }

            public DiscordSettings()
            {
                Token = Environment.GetEnvironmentVariable("TOKEN");
            }
        }
    }
}