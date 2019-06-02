using System;
using Discord;
using Microsoft.Extensions.Options;
using nhitomi.Globalization;

namespace nhitomi.Interactivity
{
    public class ErrorMessage : EmbedMessage<ErrorMessage.View>
    {
        readonly Exception _exception;

        public ErrorMessage(Exception exception)
        {
            _exception = exception;
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
                var l = Context.Localization;

                return new EmbedBuilder()
                    .WithTitle(path["title"][l])
                    .WithDescription(
                        $"`{Message._exception.Message ?? "<null>"}`\n" +
                        path["text"][l, new {invite = _settings.Discord.Guild.GuildInvite}])
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp()
                    .Build();
            }
        }
    }
}