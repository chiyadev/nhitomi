// Copyright (c) 2018-2019 chiya.dev
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
        public DiscordSettings Discord { get; set; } = new DiscordSettings();

        public sealed class DiscordSettings : DiscordSocketConfig
        {
            public string Prefix { get; set; }
            public string Token { get; set; }

            public StatusSettings Status { get; set; } = new StatusSettings();

            public sealed class StatusSettings
            {
                public double UpdateInterval { get; set; }
                public string[] Games { get; set; }
            }

            public CommandSettings Command { get; set; } = new CommandSettings();

            public sealed class CommandSettings : CommandServiceConfig
            {
                public double InteractiveExpiry { get; set; }
            }

            public GuildSettings Guild { get; set; } = new GuildSettings();

            public sealed class GuildSettings
            {
                public ulong GuildId { get; set; }
                public ulong LogChannelId { get; set; }
                public ulong LogWarningChannelId { get; set; }
                public ulong FeedCategoryId { get; set; }
                public ulong LanguageFeedCategoryId { get; set; }

                public string GuildInvite { get; set; }
            }
        }

        public DoujinSettings Doujin { get; set; } = new DoujinSettings();

        public sealed class DoujinSettings
        {
            public double FeedUpdateInterval { get; set; }
            public int MaxFeedUpdatesCount { get; set; }

            public double DownloadValidLength { get; set; }
            public bool AllowNonGuildMemberDownloads { get; set; }
        }

        public HttpSettings Http { get; set; } = new HttpSettings();

        public sealed class HttpSettings
        {
            public string Url { get; set; }

            public HttpSettings()
            {
                Url = Environment.GetEnvironmentVariable("ASPNETCORE_URLS")?.Split(';')[0]
                      ?? "https://localhost:5000";
            }
        }

        public DatabaseSettings Db { get; set; } = new DatabaseSettings();

        public sealed class DatabaseSettings
        {
            public string AccessKey { get; set; }
            public string SecretKey { get; set; }
            public string RegionEndpoint { get; set; }

            public string UserSettingsTable { get; set; }
            public string TagSubscriptionTable { get; set; }
        }
    }
}