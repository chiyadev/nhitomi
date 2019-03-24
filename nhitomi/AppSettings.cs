// Copyright (c) 2018-2019 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using Discord.Commands;
using Discord.WebSocket;

namespace nhitomi
{
    public sealed class AppSettings
    {
        public string Prefix { get; set; }
        public string CurrentEnvironment { get; set; }

        public AppSettings()
        {
            CurrentEnvironment = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "DEVELOPMENT";
        }

        public DiscordSettings Discord { get; set; }

        public sealed class DiscordSettings : DiscordSocketConfig
        {
            public string Token { get; set; }

            public DiscordSettings()
            {
                Token = Environment.GetEnvironmentVariable("TOKEN");
            }

            public StatusSettings Status { get; set; }

            public sealed class StatusSettings
            {
                public double UpdateInterval { get; set; }
                public string[] Games { get; set; }
            }

            public CommandSettings Command { get; set; }

            public sealed class CommandSettings : CommandServiceConfig
            {
                public double InteractiveExpiry { get; set; }
            }
        }

        public DoujinSettings Doujin { get; set; }

        public sealed class DoujinSettings
        {
            public double UpdateInterval { get; set; }
            public double TokenValidLength { get; set; }
        }

        public HttpSettings Http { get; set; }

        public sealed class HttpSettings
        {
            public int Port { get; set; }
            public string Url { get; set; }

            public HttpSettings()
            {
                Port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var port) ? port : 5000;
                Url = Environment.GetEnvironmentVariable("URL") ?? $"http://localhost:{Port}";
            }
        }
    }
}
