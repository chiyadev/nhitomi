using System;
using System.Text;
using Discord;
using Microsoft.Extensions.Options;
using nhitomi.Discord;
using nhitomi.Globalization;

namespace nhitomi.Interactivity
{
    public class ErrorMessage : EmbedMessage<ErrorMessage.View>
    {
        readonly Exception _exception;
        readonly bool _isDetailed;

        public ErrorMessage(Exception exception, bool isDetailed = false)
        {
            _exception = exception;
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
                var path = new LocalizationPath("errorMessage");
                var l = Context.GetLocalization();

                var embed = new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp();

                if (Message._isDetailed)
                {
                    embed.Title = path["titleAuto"][l];

                    var user = Context.User;
                    var message = Context.Message;

                    embed.AddField("Context", $@"
User: {user.Id} `{user.Username}#{user.Discriminator}`
Message: {message.Author.Id} `{message.Author.Username}#{message.Author.Discriminator}`
```
{message.Content}
```");

                    var exception = Message._exception;

                    for (var level = 0; exception != null && level < 5; level++)
                    {
                        var trace = exception.StackTrace;

                        var content = new StringBuilder()
                            .AppendLine($"Type: `{exception.GetType().FullName}`")
                            .AppendLine($"Exception: `{exception.Message}`")
                            .AppendLine("Trace:")
                            .AppendLine("```");
                        content
                            .AppendLine(trace
                                // simply cut off anything after the character limit
                                .Substring(0, Math.Min(trace.Length, _embedFieldLimit - content.Length - 4)))
                            .Append("```");

                        embed.AddField(level == 0 ? "Exception" : $"Inner exception {level}", content.ToString());

                        exception = exception.InnerException;
                    }
                }
                else
                {
                    embed.Title = path["title"][l];
                    embed.Description = new StringBuilder()
                        .AppendLine($"`{Message._exception.Message}`")
                        .AppendLine(path["text"][l, new {invite = _settings.Discord.Guild.GuildInvite}])
                        .ToString();
                }

                return embed.Build();
            }
        }
    }
}