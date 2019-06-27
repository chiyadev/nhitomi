using System;
using System.Linq;
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
        readonly AppSettings _settings;
        readonly InteractiveManager _interactive;
        readonly IServiceProvider _services;
        readonly ILogger<GalleryUrlDetector> _logger;

        public GalleryUrlDetector(IOptions<AppSettings> options,
                                  InteractiveManager interactive,
                                  IServiceProvider services,
                                  ILogger<GalleryUrlDetector> logger)
        {
            _settings    = options.Value;
            _interactive = interactive;
            _services    = services;
            _logger      = logger;
        }

        Task IMessageHandler.InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public async Task<bool> TryHandleAsync(IMessageContext context,
                                               CancellationToken cancellationToken = default)
        {
            switch (context.Event)
            {
                case MessageEvent.Create: break;

                default: return false;
            }

            var content = context.Message.Content;

            // ignore urls in commands
            if (content.StartsWith(_settings.Discord.Prefix))
                return false;

            // match gallery urls
            var ids = GalleryUtility.Parse(content);

            if (ids.Length == 0)
                return false;

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

                    if (doujin == null)
                        await context.ReplyAsync("doujinNotFound");
                    else
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