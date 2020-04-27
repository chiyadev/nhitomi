using System;
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
        readonly IUserService _users;
        readonly ICacheStore<string> _userIdCache;

        public DiscordUserHandler(IDiscordOAuthHandler oauth, IUserService users, ICacheManager cache)
        {
            _oauth       = oauth;
            _users       = users;
            _userIdCache = cache.CreateStore<string>(k => $"discord:uid:{k}", TimeSpan.FromHours(1));
        }

        public async Task SetAsync(nhitomiCommandContext context, CancellationToken cancellationToken = default)
        {
            // try getting cached db user id from discord id
            var id     = await _userIdCache.GetAsync(context.Executor.Id.ToString(), null, cancellationToken);
            var result = await _users.GetAsync(id, cancellationToken);

            if (!result.TryPickT0(out var user, out _))
            {
                // this isn't an actual oauth, but it creates users correctly
                user = await _oauth.GetOrCreateUserAsync(new DiscordOAuthUser
                {
                    Id            = context.Executor.Id,
                    Username      = context.Executor.Username,
                    Discriminator = context.Executor.DiscriminatorValue
                }, cancellationToken);

                await _userIdCache.SetAsync(context.Executor.Id.ToString(), user.Id, cancellationToken);
            }

            context.User   = user;
            context.Locale = Locales.Get(user.Language).Sections["discord"];
        }
    }
}