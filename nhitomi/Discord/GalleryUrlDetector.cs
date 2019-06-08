using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nhitomi.Core;
using nhitomi.Interactivity;

namespace nhitomi.Discord
{
    public class GalleryUrlDetector : IMessageHandler
    {
        const string _nhentai =
            @"\b((http|https):\/\/)?nhentai(\.net)?\/(g\/)?(?<source_nhentai>[0-9]{1,6})\b";

        const string _hitomi =
            @"\b((http|https):\/\/)?hitomi(\.la)?\/(galleries\/)?(?<source_Hitomi>[0-9]{1,7})\b";

        readonly AppSettings _settings;
        readonly InteractiveManager _interactive;
        readonly IServiceProvider _services;
        readonly ILogger<GalleryUrlDetector> _logger;

        readonly Regex _galleryRegex;

        public GalleryUrlDetector(IOptions<AppSettings> options, InteractiveManager interactive,
            IServiceProvider services, ILogger<GalleryUrlDetector> logger)
        {
            _settings = options.Value;
            _interactive = interactive;
            _services = services;
            _logger = logger;

            // build gallery regex to match all known formats
            _galleryRegex = new Regex(
                $"({string.Join(")|(", _nhentai, _hitomi)})",
                RegexOptions.Compiled);

            logger.LogDebug($"Gallery match regex: {_galleryRegex}");
        }

        Task IMessageHandler.InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public async Task<bool> TryHandleAsync(IMessageContext context, CancellationToken cancellationToken = default)
        {
            switch (context.Event)
            {
                case MessageEvent.Create:
                    break;

                default:
                    return false;
            }

            var content = context.Message.Content;

            // ignore urls in commands
            if (content.StartsWith(_settings.Discord.Prefix))
                return false;

            // try recognizing at least one gallery url
            if (!_galleryRegex.IsMatch(content))
                return false;

            // match gallery urls
            var ids = _galleryRegex
                .Matches(content)
                .SelectMany(m => m.Groups)
                .Where(g => g.Name != null && g.Name.StartsWith("source_"))
                .Select(g => (g.Name.Split('_', 2)[1], g.Value))
                .ToArray();

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug($"Matched galleries: {string.Join(", ", ids.Select((s, i) => $"{s}/{i}"))}");

            // send interactive
            using (context.BeginTyping())
            {
                // send one interactive if only one id detected
                if (ids.Length == 1)
                {
                    var (source, id) = ids[0];

                    Doujin doujin;

                    using (var scope = _services.CreateScope())
                    {
                        doujin = await scope.ServiceProvider
                            .GetRequiredService<IDatabase>()
                            .GetDoujinAsync(source, id, cancellationToken);
                    }

                    await _interactive.SendInteractiveAsync(
                        new DoujinMessage(doujin),
                        context,
                        cancellationToken);
                }

                // send as a list
                else
                {
                    await _interactive.SendInteractiveAsync(
                        new GalleryUrlDetectedMessage(ids),
                        context,
                        cancellationToken);
                }
            }

            return true;
        }

        sealed class GalleryUrlDetectedMessage : DoujinListMessage<GalleryUrlDetectedMessage.View>
        {
            readonly (string, string)[] _ids;

            public GalleryUrlDetectedMessage((string, string)[] ids)
            {
                _ids = ids;
            }

            public class View : DoujinListView
            {
                new GalleryUrlDetectedMessage Message => (GalleryUrlDetectedMessage) base.Message;

                readonly IDatabase _db;

                public View(IDatabase db)
                {
                    _db = db;
                }

                protected override Task<Doujin[]> GetValuesAsync(int offset,
                    CancellationToken cancellationToken = default) =>
                    _db.GetDoujinsAsync(Message._ids.Skip(offset).Take(10).ToArray());
            }
        }
    }
}