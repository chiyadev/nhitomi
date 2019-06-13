using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
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

        static async Task<bool> EnsureGuildAdminAsync(IDiscordContext context,
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

        [Command("language")]
        public async Task LanguageAsync(string language, CancellationToken cancellationToken = default)
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

                await _context.ReplyAsync("localizationChanged", new
                {
                    localization = Localization.GetLocalization(language)
                });
            }
            else
            {
                await _context.ReplyAsync("localizationNotFound", new {language});
            }
        }

        [Command("filter")]
        public async Task FilterAsync(bool enabled, CancellationToken cancellationToken = default)
        {
            if (!await EnsureGuildAdminAsync(_context, cancellationToken))
                return;

            Guild guild;

            do
            {
                guild = await _db.GetGuildAsync(_context.GuildSettings.Id, cancellationToken);

                guild.SearchQualityFilter = enabled;
            }
            while (!await _db.SaveAsync(cancellationToken));

            // update cache
            _settingsCache[_context.Channel] = guild;

            await _context.ReplyAsync("qualityFilterChanged", new {state = enabled});
        }

        [Module("feed")]
        public class FeedModule
        {
            readonly IDiscordContext _context;
            readonly IDatabase _db;

            public FeedModule(IDiscordContext context, IDatabase db)
            {
                _context = context;
                _db = db;
            }

            [Command("add"), Binding("[tag+]")]
            public async Task AddAsync(string tag, CancellationToken cancellationToken = default)
            {
                if (!await EnsureGuildAdminAsync(_context, cancellationToken))
                    return;

                var added = false;

                do
                {
                    var channel = await _db.GetFeedChannelAsync(_context.GuildSettings.Id, _context.Channel.Id,
                        cancellationToken);

                    var tags = await _db.GetTagsAsync(tag, cancellationToken);

                    if (tags.Length == 0)
                    {
                        await _context.ReplyAsync("tagNotFound", new {tag});
                        return;
                    }

                    foreach (var t in tags)
                    {
                        var tagRef = channel.Tags.FirstOrDefault(x => x.TagId == t.Id);

                        if (tagRef == null)
                        {
                            channel.Tags.Add(new FeedChannelTag
                            {
                                Tag = t
                            });
                            added = true;
                        }
                    }
                }
                while (!await _db.SaveAsync(cancellationToken));

                if (added)
                    await _context.ReplyAsync("feedTagAdded", new {tag, channel = _context.Channel});
                else
                    await _context.ReplyAsync("feedTagAlreadyAdded", new {tag, channel = _context.Channel});
            }

            [Command("remove"), Binding("[tag+]")]
            public async Task RemoveAsync(string tag, CancellationToken cancellationToken = default)
            {
                if (!await EnsureGuildAdminAsync(_context, cancellationToken))
                    return;

                var removed = false;

                do
                {
                    var channel = await _db.GetFeedChannelAsync(_context.GuildSettings.Id, _context.Channel.Id,
                        cancellationToken);

                    foreach (var t in await _db.GetTagsAsync(tag, cancellationToken))
                    {
                        var tagRef = channel.Tags.FirstOrDefault(x => x.TagId == t.Id);

                        if (tagRef != null)
                        {
                            channel.Tags.Remove(tagRef);
                            removed = true;
                        }
                    }
                }
                while (!await _db.SaveAsync(cancellationToken));

                if (removed)
                    await _context.ReplyAsync("feedTagRemoved", new {tag, channel = _context.Channel});
                else
                    await _context.ReplyAsync("feedTagNotRemoved", new {tag, channel = _context.Channel});
            }

            [Command("mode")]
            public async Task ModeAsync(FeedChannelWhitelistType type, CancellationToken cancellationToken = default)
            {
                if (!await EnsureGuildAdminAsync(_context, cancellationToken))
                    return;

                do
                {
                    var channel = await _db.GetFeedChannelAsync(_context.GuildSettings.Id, _context.Channel.Id,
                        cancellationToken);

                    channel.WhitelistType = type;
                }
                while (!await _db.SaveAsync(cancellationToken));

                await _context.ReplyAsync("feedModeChanged", new {type, channel = _context.Channel});
            }
        }
    }
}