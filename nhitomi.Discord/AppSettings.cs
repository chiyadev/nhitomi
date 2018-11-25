using Discord.Commands;
using Discord.WebSocket;

namespace nhitomi
{
    public sealed class AppSettings
    {
        public string Prefix { get; set; }

        public DiscordSettings Discord { get; set; }
        public sealed class DiscordSettings : DiscordSocketConfig
        {
            public string Token { get; set; }

            public ServerSettings Server { get; set; }
            public sealed class ServerSettings
            {
                public ulong ServerId { get; set; }
                public ulong LogChannelId { get; set; }
            }

            public StatusSettings Status { get; set; }
            public sealed class StatusSettings
            {
                public int UpdateInterval { get; set; }
                public string[] Games { get; set; }
            }

            public CommandSettings Command { get; set; }
            public sealed class CommandSettings : CommandServiceConfig
            {
                public int InteractiveExpiry { get; set; }
            }
        }

        public DoujinSettings Doujin { get; set; }
        public sealed class DoujinSettings
        {
            public int UpdateInterval { get; set; }
        }
    }
}