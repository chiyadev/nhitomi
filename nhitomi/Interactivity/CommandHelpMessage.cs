using System.Collections.Generic;
using System.Linq;
using Discord;
using Microsoft.Extensions.Options;

namespace nhitomi.Interactivity
{
    public class CommandHelpMessage : EmbedMessage<CommandHelpMessage.View>
    {
        public string Command { get; set; }
        public string[] Aliases { get; set; }
        public string Description { get; set; }
        public string[] Examples { get; set; }

        public class View : EmbedViewBase
        {
            CommandHelpMessage Message => (CommandHelpMessage) base.Message;

            readonly AppSettings _settings;

            public View(IOptions<AppSettings> options)
            {
                _settings = options.Value;
            }

            protected override Embed CreateEmbed()
            {
                var command = Message.Command;
                var prefix  = _settings.Discord.Prefix;

                return new EmbedBuilder
                {
                    Title       = $"**nhitomi**: {prefix}{command}",
                    Color       = Color.Purple,
                    Description = Message.Description,

                    Fields = new List<EmbedFieldBuilder>
                    {
                        new EmbedFieldBuilder
                        {
                            Name  = "Aliases",
                            Value = string.Join(", ", Message.Aliases.Append(command).Select(s => $"`{prefix}{s}`"))
                        },
                        new EmbedFieldBuilder
                        {
                            Name  = "Examples",
                            Value = string.Join(", ", Message.Examples.Select(s => $"`{prefix}{s}`"))
                        }
                    },

                    Footer = new EmbedFooterBuilder
                    {
                        Text = "v3.2 Heresta - powered by chiya.dev"
                    }
                }.Build();
            }
        }
    }
}