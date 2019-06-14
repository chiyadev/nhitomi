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
User: {user.Username}#{user.DiscriminatorValue} `{user.Id}`
Message: by {message.Author.Username}#{message.Author.DiscriminatorValue} `{message.Author.Id}`
```
{message.Content}
```");

                    var exception = Message._exception;

                    for (var level = 0; exception != null; level++)
                    {
                        embed.AddField(level == 0 ? "Exception" : $"Inner exception {level}", $@"
Type: `{exception.GetType().FullName}`
Exception: `{exception.Message}`
Stack trace:
```
{exception.StackTrace.Replace("```", "`")}
```");

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