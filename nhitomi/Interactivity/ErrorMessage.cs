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

        protected override void InitializeView(View view)
        {
            base.InitializeView(view);

            view.Exception = _exception;
        }

        public class View : EmbedViewBase
        {
            public Exception Exception;

            readonly AppSettings _settings;

            public View(IOptions<AppSettings> options)
            {
                _settings = options.Value;
            }

            protected override Embed CreateEmbed() => new EmbedBuilder()
                .WithTitle("**nhitomi**: Error")
                .WithDescription(
                    $"`{Exception.Message ?? "<null>"}`\n" +
                    $"Error has been reported. For further assistance, please join <{_settings.Discord.Guild.GuildInvite}>")
                .WithColor(Color.Red)
                .WithCurrentTimestamp()
                .Build();
        }
    }
}
