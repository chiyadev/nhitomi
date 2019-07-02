using System;
using System.Text;
using Discord;
using Microsoft.Extensions.Options;
using nhitomi.Discord;

namespace nhitomi.Interactivity
{
    public class ErrorMessage : EmbedMessage<ErrorMessage.View>
    {
        readonly Exception _exception;
        readonly bool _isDetailed;

        public ErrorMessage(Exception exception,
                            bool isDetailed = false)
        {
            _exception  = exception;
            _isDetailed = isDetailed;
        }

        public class View : EmbedViewBase
        {
            new ErrorMessage Message => (ErrorMessage) base.Message;

            readonly AppSettings _settings;

            public View(IOptions<AppSettings> options)
            {
                _settings = options.Value;
            }

            /// <summary>
            /// 1024 character limit on embed fields.
            /// </summary>
            const int _embedFieldLimit = 1024;

            protected override Embed CreateEmbed()
            {
                var l = Context.GetLocalization()["errorMessage"];

                var embed = new EmbedBuilder
                {
                    Color = Color.Red
                };

                embed.WithCurrentTimestamp();

                if (!Message._isDetailed)
                {
                    embed.Title = l["title"];

                    embed.Description =
                        new StringBuilder()
                           .AppendLine($"`{Message._exception.Message}`")
                           .AppendLine(l["text", new { invite = _settings.Discord.Guild.GuildInvite }])
                           .ToString();
                }
                else
                {
                    embed.Title = l["titleAuto"];

                    var user    = Context.User;
                    var message = Context.Message;

                    embed.AddField("Context",
                                   $@"
User: {user.Id} `{user.Username}#{user.Discriminator}`
Message: {message.Author.Id} `{message.Author.Username}#{message.Author.Discriminator}`
```
{message.Content}
```
".Trim());

                    var exception = Message._exception;

                    for (var level = 0; exception != null && level < 5; level++)
                    {
                        var content = new StringBuilder()
                                     .AppendLine($"Type: `{exception.GetType().FullName}`")
                                     .AppendLine($"Exception: `{exception.Message}`")
                                     .AppendLine("```");

                        var trace = exception.StackTrace;

                        // simply cut off anything after the character limit
                        trace = trace.Substring(0, Math.Min(trace.Length, _embedFieldLimit - content.Length - 4));

                        content
                           .AppendLine(trace)
                           .Append("```");

                        embed.AddField(level == 0 ? "Exception" : $"Inner exception {level}", content.ToString());

                        // traverse inner exceptions
                        exception = exception.InnerException;
                    }
                }

                return embed.Build();
            }
        }
    }
}