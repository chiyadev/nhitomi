using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using nhitomi.Core;
using nhitomi.Discord;
using nhitomi.Discord.Parsing;
using nhitomi.Interactivity;

namespace nhitomi.Modules
{
    [Module("doujin", IsPrefixed = false)]
    public class DoujinModule
    {
        readonly IMessageContext _context;
        readonly AppSettings _settings;
        readonly IDatabase _database;
        readonly InteractiveManager _interactive;

        public DoujinModule(IMessageContext context, IOptions<AppSettings> options, IDatabase database,
            InteractiveManager interactive)
        {
            _context = context;
            _settings = options.Value;
            _database = database;
            _interactive = interactive;
        }

        [Command("get")]
        public async Task GetAsync(string source, string id, CancellationToken cancellationToken = default)
        {
            var doujin = await _database.GetDoujinAsync(source, id, cancellationToken);

            if (doujin == null)
            {
                await _context.ReplyAsync("messages.doujinNotFound");
                return;
            }

            await _interactive.SendInteractiveAsync(new DoujinMessage(doujin), _context, cancellationToken);
        }

        [Command("from")]
        public Task FromAsync(string source, CancellationToken cancellationToken = default) =>
            _interactive.SendInteractiveAsync(new DoujinListFromSourceMessage(source), _context, cancellationToken);

        [Command("search"), Binding("[query+]")]
        public async Task SearchAsync(string query, bool? filter = null, string source = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(query))
            {
                await _context.ReplyAsync("messages.invalidQuery", new {query});
                return;
            }

            await _interactive.SendInteractiveAsync(
                new DoujinListFromQueryMessage(query)
                {
                    QualityFilter = filter ?? _context.GuildSettings.SearchQualityFilter ?? false,
                    Source = source
                },
                _context,
                cancellationToken);
        }

        [Command("download", Aliases = new[] {"dl"})]
        public async Task DownloadAsync(string source, string id, CancellationToken cancellationToken = default)
        {
            // allow downloading only for users of guild
            if (!_settings.Doujin.AllowNonGuildMemberDownloads)
            {
                var guild = await _context.Client.GetGuildAsync(_settings.Discord.Guild.GuildId);

                // guild user is null; user is not in guild
                if (guild != null && await guild.GetUserAsync(_context.User.Id) == null)
                {
                    await _context.ReplyAsync("messages.joinForDownload",
                        new {invite = _settings.Discord.Guild.GuildInvite});
                    return;
                }
            }

            var doujin = await _database.GetDoujinAsync(source, id, cancellationToken);

            if (doujin == null)
            {
                await _context.ReplyAsync("messages.doujinNotFound");
                return;
            }

            await _interactive.SendInteractiveAsync(new DownloadMessage(doujin), _context, cancellationToken);
        }
    }
}