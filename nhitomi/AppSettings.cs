using Discord.WebSocket;

namespace nhitomi
{
    public sealed class AppSettings
    {
        public string ImageUrl { get; set; }

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

        public ApiSettings Api { get; set; } = new ApiSettings();

        public sealed class ApiSettings
        {
            public string BaseUrl { get; set; }
            public string AuthToken { get; set; }
        }

        public DoujinSettings Doujin { get; set; } = new DoujinSettings();

        public sealed class DoujinSettings
        {
            public bool AllowNonGuildMemberDownloads { get; set; }
        }

        public HttpSettings Http { get; set; } = new HttpSettings();

        public sealed class HttpSettings
        {
            public bool EnableProxy { get; set; }
        }
    }
}