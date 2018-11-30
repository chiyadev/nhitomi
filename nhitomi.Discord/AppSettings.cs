using System;
using Discord.Commands;
using Discord.WebSocket;

namespace nhitomi
{
    public sealed class AppSettings
    {
        public string Prefix { get; set; }
        public string CurrentEnvironment { get; set; } = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "DEVELOPMENT";

        public DiscordSettings Discord { get; set; }
        public sealed class DiscordSettings : DiscordSocketConfig
        {
            public string Token { get; set; } = Environment.GetEnvironmentVariable("TOKEN");

            public ServerSettings Server { get; set; }
            public sealed class ServerSettings
            {
                public ulong ServerId { get; set; }
                public ulong LogChannelId { get; set; }
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
        }

        public HttpSettings Http { get; set; }
        public sealed class HttpSettings
        {
            public int Port { get; set; } = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var port) ? port : 5000;
            public int Concurrency { get; set; }
        }
    }
}