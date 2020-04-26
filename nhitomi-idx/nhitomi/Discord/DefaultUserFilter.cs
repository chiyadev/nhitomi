using Discord;

namespace nhitomi.Discord
{
    public interface IUserFilter
    {
        bool HandleUser(IUser user);
    }

    /// <summary>
    /// Allows non-bot non-webhook users.
    /// </summary>
    public class DefaultUserFilter : IUserFilter
    {
        public bool HandleUser(IUser user) => !user.IsBot && !user.IsWebhook;
    }
}