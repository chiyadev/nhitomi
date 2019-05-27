using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace nhitomi.Interactivity
{
    public class ErrorMessage : EmbedMessage
    {
        protected override async Task UpdateViewAsync(CancellationToken cancellationToken = default)
        {
            var settings = Services.GetRequiredService<IOptions<AppSettings>>().Value;

            var embed = new EmbedBuilder()
                .WithTitle("**nhitomi**: Error")
                .WithDescription(
                    "Sorry, we encountered an unexpected error and have reported it to the developers! " +
                    $"Please join our official server for further assistance: {settings.Discord.Guild.GuildInvite}")
                .WithColor(Color.Red)
                .WithCurrentTimestamp()
                .Build();

            await SetViewAsync(embed, cancellationToken);
        }
    }
}