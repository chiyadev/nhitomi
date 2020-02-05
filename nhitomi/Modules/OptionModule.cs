using System.Threading;
using System.Threading.Tasks;
using Discord;
using nhitomi.Core;
using nhitomi.Discord;
using nhitomi.Discord.Parsing;
using nhitomi.Globalization;
using nhitomi.Interactivity;

namespace nhitomi.Modules
{
    [Module("option", Alias = "o")]
    public partial class OptionModule
    {
        readonly IDiscordContext _context;
        readonly IDatabase _db;
        readonly GuildSettingsCache _settingsCache;
        readonly InteractiveManager _interactive;

        public OptionModule(IDiscordContext context,
                            IDatabase db,
                            GuildSettingsCache settingsCache,
                            InteractiveManager interactive)
        {
            _context       = context;
            _db            = db;
            _settingsCache = settingsCache;
            _interactive   = interactive;
        }

        public static async Task<bool> EnsureGuildAdminAsync(IDiscordContext context,
                                                             CancellationToken cancellationToken = default)
        {
            if (!(context.User is IGuildUser user))
            {
                await context.ReplyAsync("commandInvokeNotInGuild");
                return false;
            }

            if (!user.GuildPermissions.ManageGuild)
            {
                await context.ReplyAsync("notGuildAdmin");
                return false;
            }

            return true;
        }

        [Command("language", Alias = "l")]
        public async Task LanguageAsync(string language,
                                        CancellationToken cancellationToken = default)
        {
            if (!await EnsureGuildAdminAsync(_context, cancellationToken))
                return;

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

                // respond in the new language
                await new DiscordContextWrapper(_context) { GuildSettings = guild }
                   .ReplyAsync("localizationChanged",
                               new
                               {
                                   localization = Localization.GetLocalization(language)
                               });
            }
            else
            {
                await _context.ReplyAsync("localizationNotFound", new { language });
            }
        }

        [Command("language")]
        public Task LanguageAsync(CancellationToken cancellationToken = default) => _interactive.SendInteractiveAsync(
            new CommandHelpMessage
            {
                Command        = "language",
                Aliases        = new[] { "l" },
                DescriptionKey = "options.language",
                Examples = new[]
                {
                    "en",
                    "english"
                }
            },
            _context,
            cancellationToken);
    }
}