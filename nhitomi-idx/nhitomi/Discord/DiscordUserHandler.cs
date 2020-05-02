using System.Threading;
using System.Threading.Tasks;
using nhitomi.Controllers;
using nhitomi.Database;
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
        readonly IRedisClient _redis;

        public DiscordUserHandler(IDiscordOAuthHandler oauth, IUserService users, IRedisClient redis)
        {
            _oauth = oauth;
            _users = users;
            _redis = redis;
        }

        public async Task SetAsync(nhitomiCommandContext context, CancellationToken cancellationToken = default)
        {
            // try getting cached db user id from discord id
            var idCache = $"discord:uid:{context.Executor.Id}";

            var id     = await _redis.GetObjectAsync<string>(idCache, cancellationToken);
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

                await _redis.SetObjectAsync(idCache, user.Id, cancellationToken: cancellationToken);
            }

            context.User   = user;
            context.Locale = Locales.Get(user.Language).Sections["discord"];
        }
    }
}