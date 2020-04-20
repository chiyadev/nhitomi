using System.Threading;
using System.Threading.Tasks;
using nhitomi.Localization;

namespace nhitomi.Discord
{
    public interface IDiscordLocaleProvider
    {
        Task SetAsync(nhitomiCommandContext context, CancellationToken cancellationToken = default);
    }

    public class DiscordLocaleProvider : IDiscordLocaleProvider
    {
        public Task SetAsync(nhitomiCommandContext context, CancellationToken cancellationToken = default)
        {
            context.Locale = Locales.Default.Sections["discord"];

            return Task.CompletedTask;
        }
    }
}