using System;
using Discord.WebSocket;
using Qmmands;

namespace nhitomi.Discord
{
    public class DiscordOptions : DiscordSocketConfig
    {
        /// <summary>
        /// True to enable the Discord subsystem.
        /// </summary>
        public bool Enabled { get; set; } = true;

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
        }
    }
}