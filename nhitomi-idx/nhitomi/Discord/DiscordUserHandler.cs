using System.Threading;
using System.Threading.Tasks;
using nhitomi.Controllers;
using nhitomi.Localization;

namespace nhitomi.Discord
{
    public interface IDiscordUserHandler
    {
        Task SetAsync(nhitomiCommandContext context, CancellationToken cancellationToken = default);
    }

    public class DiscordUserHandler : IDiscordUserHandler
    {
        readonly IDiscordOAuthHandler _oauth;

        public DiscordUserHandler(IDiscordOAuthHandler oauth)
        {
            _oauth = oauth;
        }

        public async Task SetAsync(nhitomiCommandContext context, CancellationToken cancellationToken = default)
        {
            // this isn't an actual oauth, but it creates users correctly
            var user = await _oauth.GetOrCreateUserAsync(new DiscordOAuthUser
            {
                Id            = context.Executor.Id,
                Username      = context.Executor.Username,
                Discriminator = context.Executor.DiscriminatorValue
            }, cancellationToken);

            context.User   = user;
            context.Locale = Locales.Get(user.Language).Sections["discord"];
        }
    }
}