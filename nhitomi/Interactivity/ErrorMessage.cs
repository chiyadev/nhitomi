using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace nhitomi.Interactivity
{
    public class ErrorMessage : EmbedMessage
    {
        readonly Exception _exception;

        public ErrorMessage(Exception exception)
        {
            _exception = exception;
        }

        protected override async Task<bool> InitializeViewAsync(IServiceProvider services,
            CancellationToken cancellationToken = default)
        {
            var settings = services.GetRequiredService<IOptions<AppSettings>>().Value;

            var embed = new EmbedBuilder()
                .WithTitle("**nhitomi**: Error")
                .WithDescription(
                    $"Message: `{_exception.Message ?? "<null>"}`\n" +
                    $"Error has been reported. For further assistance, please join <{settings.Discord.Guild.GuildInvite}>")
                .WithColor(Color.Red)
                .WithCurrentTimestamp()
                .Build();

            await SetViewAsync(embed, cancellationToken);

            return true;
        }
    }
}