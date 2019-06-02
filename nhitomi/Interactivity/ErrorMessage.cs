using System;
using Discord;
using Microsoft.Extensions.Options;

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

            protected override Embed CreateEmbed() => new EmbedBuilder()
                .WithTitle("**nhitomi**: Error")
                .WithDescription(
                    $"`{Message._exception.Message ?? "<null>"}`\n" +
                    $"Error has been reported. For further assistance, please join <{_settings.Discord.Guild.GuildInvite}>")
                .WithColor(Color.Red)
                .WithCurrentTimestamp()
                .Build();
        }
    }
}