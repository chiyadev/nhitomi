using System.Threading;
using System.Threading.Tasks;
using nhitomi.Core;
using nhitomi.Discord;
using nhitomi.Discord.Parsing;
using nhitomi.Globalization;

namespace nhitomi.Modules
{
    [Module("option")]
    public class OptionModule
    {
        readonly IDiscordContext _context;
        readonly IDatabase _db;
        readonly GuildSettingsCache _settingsCache;

        public OptionModule(IDiscordContext context, IDatabase db, GuildSettingsCache settingsCache)
        {
            _context = context;
            _db = db;
            _settingsCache = settingsCache;
        }

        [Command("language")]
        public async Task LanguageAsync(string language, CancellationToken cancellationToken = default)
        {
            // ensure language exists
            if (Localization.IsAvailable(language))
            {
                Guild guild;

                do
                {
                    guild = await _db.GetGuildAsync(_context.GuildSettings.Id, cancellationToken);

                    guild.Language = language;
                }
                while (!await _db.SaveAsync(cancellationToken));

                // update cache
                _settingsCache[_context.Channel] = guild;

                await _context.ReplyAsync("messages.localizationChanged", new
                {
                    localization = Localization.GetLocalization(language)
                });
            }
            else
            {
                await _context.ReplyAsync("messages.localizationNotFound");
            }
        }

        [Command("filter")]
        public async Task FilterAsync(bool enabled, CancellationToken cancellationToken = default)
        {
            Guild guild;

            do
            {
                guild = await _db.GetGuildAsync(_context.GuildSettings.Id, cancellationToken);

                guild.SearchQualityFilter = enabled;
            }
            while (!await _db.SaveAsync(cancellationToken));

            // update cache
            _settingsCache[_context.Channel] = guild;

            await _context.ReplyAsync("messages.filterChanged", new {enabled});
        }
    }
}