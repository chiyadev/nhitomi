using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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

        readonly InteractiveManager _interactive;
        readonly ILogger<GalleryUrlDetector> _logger;

        readonly Regex _galleryRegex;

        public GalleryUrlDetector(InteractiveManager interactive, ILogger<GalleryUrlDetector> logger)
        {
            _interactive = interactive;
            _logger = logger;

            // build gallery regex to match all known formats
            _galleryRegex = new Regex(
                $"({string.Join(")|(", _nhentai, _hitomi)})",
                RegexOptions.Compiled);

            logger.LogDebug($"Gallery match regex: {_galleryRegex}");
        }

        Task IMessageHandler.InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public async Task<bool> TryHandleAsync(MessageContext context, CancellationToken cancellationToken = default)
        {
            switch (context.Event)
            {
                case MessageEvent.Create:
                    break;

                default:
                    return false;
            }

            var content = context.Message.Content;

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

            IAsyncEnumerable<Doujin> enumerate(IDatabase db, int offset) => db.GetDoujinsAsync(ids);

            // send interactive
            await _interactive.SendInteractiveAsync(new DoujinListMessage(enumerate), context, cancellationToken);

            return true;
        }
    }
}