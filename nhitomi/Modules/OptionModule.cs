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
            var guild = await _db.GetGuildAsync(_context.GuildSettings.Id, cancellationToken);

            // ensure language exists
            if (Localization.IsAvailable(language))
            {
                guild.Language = language;

                await _db.SaveAsync(cancellationToken);

                await _context.ReplyAsync("messages.localizationChanged", new
                {
                    localization = Localization.GetLocalization(language)
                });

                // update cache
                _settingsCache[_context.Channel] = guild;
            }
            else
            {
                await _context.ReplyAsync("messages.localizationNotFound");
            }
        }
    }
}