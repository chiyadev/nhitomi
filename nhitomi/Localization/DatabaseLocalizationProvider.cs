using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;
using nhitomi.Core;

namespace nhitomi.Localization
{
    public interface ILocalization
    {
        Task<Localization> GetLocalizationAsync(ICommandContext context, CancellationToken cancellationToken = default);
    }

    public class DatabaseLocalizationProvider : ILocalization
    {
        readonly IDatabase _db;

        public DatabaseLocalizationProvider(IDatabase db)
        {
            _db = db;
        }

        public async Task<Localization> GetLocalizationAsync(ICommandContext context,
            CancellationToken cancellationToken = default)
        {
            //todo: CACHE THE RESULT
            var user = await _db.GetUserAsync(context.User.Id, cancellationToken);

            return Localization.GetLocalization(user.Language);
        }
    }
}