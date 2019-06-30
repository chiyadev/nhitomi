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

        public DoujinModule(IMessageContext context,
                            IOptions<AppSettings> options,
                            IDatabase database,
                            InteractiveManager interactive)
        {
            _context     = context;
            _settings    = options.Value;
            _database    = database;
            _interactive = interactive;
        }

        [Command("get", Alias = "g")]
        public async Task GetAsync(string source,
                                   string id,
                                   CancellationToken cancellationToken = default)
        {
            var doujin = await _database.GetDoujinAsync(GalleryUtility.ExpandContraction(source),
                                                        id,
                                                        cancellationToken);

            if (doujin == null)
            {
                await _context.ReplyAsync("doujinNotFound");
                return;
            }

            await _interactive.SendInteractiveAsync(new DoujinMessage(doujin), _context, cancellationToken);
        }

        [Command("get", Alias = "g")]
        public Task GetAsync(string url,
                             CancellationToken cancellationToken = default)
        {
            var (source, id) = GalleryUtility.Parse(url);

            return GetAsync(source, id, cancellationToken);
        }

        [Command("get")]
        public Task GetAsync(CancellationToken cancellationToken = default) => _interactive.SendInteractiveAsync(
            new CommandHelpMessage
            {
                Command        = "get",
                Aliases        = new[] { "g" },
                DescriptionKey = "doujins.get",
                Examples       = CommandHelpMessage.DoujinCommandExamples
            },
            _context,
            cancellationToken);

        [Command("from", Alias = "f")]
        public Task FromAsync(string source,
                              CancellationToken cancellationToken = default) => _interactive.SendInteractiveAsync(
            new DoujinListFromSourceMessage(source),
            _context,
            cancellationToken);

        [Command("from")]
        public Task FromAsync(CancellationToken cancellationToken = default) => _interactive.SendInteractiveAsync(
            new CommandHelpMessage
            {
                Command        = "from",
                Aliases        = new[] { "f" },
                DescriptionKey = "doujins.from",
                Examples = new[]
                {
                    "nhentai",
                    "hitomi"
                }
            },
            _context,
            cancellationToken);

        [Command("search", Alias = "s"), Binding("[query+]")]
        public async Task SearchAsync(string query,
                                      string source = null,
                                      CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(query))
            {
                await _context.ReplyAsync("invalidQuery", new { query });
                return;
            }

            await _interactive.SendInteractiveAsync(
                new DoujinListFromQueryMessage(new DoujinSearchArgs
                {
                    Query         = query,
                    QualityFilter = false,
                    Source        = GalleryUtility.ExpandContraction(source)
                }),
                _context,
                cancellationToken);
        }

        [Command("search")]
        public Task SearchAsync(CancellationToken cancellationToken = default) => _interactive.SendInteractiveAsync(
            new CommandHelpMessage
            {
                Command        = "search",
                Aliases        = new[] { "s" },
                DescriptionKey = "doujins.search",
                Examples = new[]
                {
                    "full color",
                    "glasses",
                    "big breasts"
                }
            },
            _context,
            cancellationToken);

        [Command("download", Alias = "dl")]
        public async Task DownloadAsync(string source,
                                        string id,
                                        CancellationToken cancellationToken = default)
        {
            var doujin = await _database.GetDoujinAsync(GalleryUtility.ExpandContraction(source),
                                                        id,
                                                        cancellationToken);

            if (doujin == null)
            {
                await _context.ReplyAsync("doujinNotFound");
                return;
            }

            await _interactive.SendInteractiveAsync(new DownloadMessage(doujin), _context, cancellationToken);
        }

        [Command("download", Alias = "dl")]
        public Task DownloadAsync(string url,
                                  CancellationToken cancellationToken = default)
        {
            var (source, id) = GalleryUtility.Parse(url);

            return DownloadAsync(source, id, cancellationToken);
        }

        [Command("download")]
        public Task DownloadAsync(CancellationToken cancellationToken = default) => _interactive.SendInteractiveAsync(
            new CommandHelpMessage
            {
                Command        = "download",
                Aliases        = new[] { "dl" },
                DescriptionKey = "doujins.download",
                Examples       = CommandHelpMessage.DoujinCommandExamples
            },
            _context,
            cancellationToken);

        [Command("read", Alias = "r")]
        public async Task ReadAsync(string source,
                                    string id,
                                    CancellationToken cancellationToken = default)
        {
            var doujin = await _database.GetDoujinAsync(GalleryUtility.ExpandContraction(source),
                                                        id,
                                                        cancellationToken);

            if (doujin == null)
            {
                await _context.ReplyAsync("doujinNotFound");
                return;
            }

            await _interactive.SendInteractiveAsync(new DoujinReadMessage(doujin), _context, cancellationToken);
        }

        [Command("read", Alias = "r")]
        public Task ReadAsync(string url,
                              CancellationToken cancellationToken = default)
        {
            var (source, id) = GalleryUtility.Parse(url);

            return ReadAsync(source, id, cancellationToken);
        }

        [Command("read")]
        public Task ReadAsync(CancellationToken cancellationToken = default) => _interactive.SendInteractiveAsync(
            new CommandHelpMessage
            {
                Command        = "read",
                Aliases        = new[] { "r" },
                DescriptionKey = "doujins.read",
                Examples       = CommandHelpMessage.DoujinCommandExamples
            },
            _context,
            cancellationToken);
    }
}