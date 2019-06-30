using System.Collections.Generic;
using System.Linq;
using Discord;
using Microsoft.Extensions.Options;
using nhitomi.Discord;

namespace nhitomi.Interactivity
{
    public class CommandHelpMessage : EmbedMessage<CommandHelpMessage.View>
    {
        public string Title { get; set; }
        public string Command { get; set; }
        public string[] Aliases { get; set; }
        public string DescriptionKey { get; set; }
        public string[] Examples { get; set; }

        public class View : EmbedViewBase
        {
            new CommandHelpMessage Message => (CommandHelpMessage) base.Message;

            readonly AppSettings _settings;

            public View(IOptions<AppSettings> options)
            {
                _settings = options.Value;
            }

            protected override Embed CreateEmbed()
            {
                var l       = Context.GetLocalization()["helpMessage"];
                var command = Message.Command;
                var prefix  = _settings.Discord.Prefix;

                return new EmbedBuilder
                {
                    Title       = $"**nhitomi**: {prefix}{Message.Title ?? command}",
                    Color       = Color.Purple,
                    Description = l[Message.DescriptionKey],

                    Fields = new List<EmbedFieldBuilder>
                    {
                        new EmbedFieldBuilder
                        {
                            Name  = l["aliases"],
                            Value = string.Join(", ", Message.Aliases.Prepend(command).Select(s => $"`{prefix}{s}`"))
                        },
                        new EmbedFieldBuilder
                        {
                            Name  = l["examples"],
                            Value = string.Join('\n', Message.Examples.Select(s => $"`{prefix}{command} {s}`"))
                        }
                    }
                }.Build();
            }
        }
    }
}