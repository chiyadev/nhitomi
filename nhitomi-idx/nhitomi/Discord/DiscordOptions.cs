using System;
using Discord;
using Discord.WebSocket;
using Qmmands;

namespace nhitomi.Discord
{
    public class DiscordOptions : DiscordSocketConfig
    {
        public DiscordOptions()
        {
            // verbose by default; log messages will get filtered again by asp.net core loggers
            LogLevel = LogSeverity.Verbose;
        }

        /// <summary>
        /// Discord bot token.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Command parser options.
        /// </summary>
        public CommandServiceConfiguration Command { get; set; } = new CommandServiceConfiguration
        {
            StringComparison      = StringComparison.OrdinalIgnoreCase,
            DefaultRunMode        = RunMode.Sequential,
            SeparatorRequirement  = SeparatorRequirement.SeparatorOrWhitespace,
            IgnoresExtraArguments = true
        };

        /// <summary>
        /// Prefix string of commands.
        /// </summary>
        public string Prefix { get; set; } = "n!";

        /// <summary>
        /// Options for OAuth2 authentication. This may be null.
        /// </summary>
        public OAuthOptions OAuth { get; set; }

        public class OAuthOptions
        {
            /// <summary>
            /// Bot client ID.
            /// </summary>
            public ulong ClientId { get; set; }

            /// <summary>
            /// Bot client secret.
            /// </summary>
            public string ClientSecret { get; set; }

            /// <summary>
            /// Redirect URI configured for OAuth.
            /// </summary>
            public string RedirectUri { get; set; }

            /// <summary>
            /// Required OAuth scopes.
            /// </summary>
            public string[] Scopes { get; set; } = { "identify", "email" };
        }

        /// <summary>
        /// Options for interactive messages.
        /// </summary>
        public InteractiveOptions Interactive { get; set; } = new InteractiveOptions();

        public class InteractiveOptions
        {
            /// <summary>
            /// Maximum number of interactive messages to keep in memory.
            /// </summary>
            public int MaxMessages { get; set; } = 512;

            /// <summary>
            /// Time after which an interactive message will be expired.
            /// </summary>
            public TimeSpan Expiry { get; set; } = TimeSpan.FromHours(6);

            /// <summary>
            /// True to allow once expired interactive messages to be resurrected.
            /// This is only supported on stateless interactive messages.
            /// </summary>
            public bool AllowResurrection { get; set; } = true;

            /// <summary>
            /// Interval of interactive rerenders.
            /// </summary>
            public TimeSpan RenderInterval { get; set; } = TimeSpan.FromSeconds(1);
        }

        /// <summary>
        /// Server invite link.
        /// </summary>
        public string ServerInvite { get; set; } = "https://discord.gg/JFNga7q";

        /// <summary>
        /// Required permissions when inviting nhitomi to a server.
        /// </summary>
        public int BotInvitePerms { get; set; } = 347200;
    }
}