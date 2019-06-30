using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using nhitomi.Core;
using nhitomi.Discord;
using nhitomi.Discord.Parsing;

namespace nhitomi.Modules
{
    public partial class OptionModule
    {
        [Module("feed", Alias = "f")]
        public class FeedModule
        {
            readonly AppSettings _settings;
            readonly IDiscordContext _context;
            readonly IDatabase _db;

            public FeedModule(IOptions<AppSettings> options,
                              IDiscordContext context,
                              IDatabase db)
            {
                _settings = options.Value;
                _context  = context;
                _db       = db;
            }

            async Task<bool> EnsureFeedEnabled(CancellationToken cancellationToken = default)
            {
                if (_settings.Feed.Enabled)
                    return true;

                await _context.ReplyAsync("**nhitomi**: Feed channels are currently disabled.");
                return false;
            }

            [Command("add", Alias = "a"), Binding("[tag+]")]
            public async Task AddAsync(string tag,
                                       CancellationToken cancellationToken = default)
            {
                if (!await EnsureFeedEnabled(cancellationToken) ||
                    !await EnsureGuildAdminAsync(_context, cancellationToken))
                    return;

                var added = false;

                do
                {
                    var channel = await _db.GetFeedChannelAsync(
                        _context.GuildSettings.Id,
                        _context.Channel.Id,
                        cancellationToken);

                    var tags = await _db.GetTagsAsync(tag, cancellationToken);

                    if (tags.Length == 0)
                    {
                        await _context.ReplyAsync("tagNotFound", new { tag });
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
                    await _context.ReplyAsync("feedTagAdded", new { tag, channel = _context.Channel });
                else
                    await _context.ReplyAsync("feedTagAlreadyAdded", new { tag, channel = _context.Channel });
            }

            [Command("remove", Alias = "r"), Binding("[tag+]")]
            public async Task RemoveAsync(string tag,
                                          CancellationToken cancellationToken = default)
            {
                if (!await EnsureFeedEnabled(cancellationToken) ||
                    !await EnsureGuildAdminAsync(_context, cancellationToken))
                    return;

                var removed = false;

                do
                {
                    var channel = await _db.GetFeedChannelAsync(_context.GuildSettings.Id,
                                                                _context.Channel.Id,
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
                    await _context.ReplyAsync("feedTagRemoved", new { tag, channel = _context.Channel });
                else
                    await _context.ReplyAsync("feedTagNotRemoved", new { tag, channel = _context.Channel });
            }

            [Command("mode", Alias = "m")]
            public async Task ModeAsync(FeedChannelWhitelistType type,
                                        CancellationToken cancellationToken = default)
            {
                if (!await EnsureFeedEnabled(cancellationToken) ||
                    !await EnsureGuildAdminAsync(_context, cancellationToken))
                    return;

                do
                {
                    var channel = await _db.GetFeedChannelAsync(_context.GuildSettings.Id,
                                                                _context.Channel.Id,
                                                                cancellationToken);

                    channel.WhitelistType = type;
                }
                while (!await _db.SaveAsync(cancellationToken));

                await _context.ReplyAsync("feedModeChanged", new { type, channel = _context.Channel });
            }
        }
    }
}