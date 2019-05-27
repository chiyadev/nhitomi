using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using nhitomi.Core;

namespace nhitomi.Interactivity.Triggers
{
    public class DownloadTrigger : ReactionTrigger<IDoujinInteractive>
    {
        public override string Name => "Download";
        public override IEmote Emote => new Emoji("\uD83D\uDCBE");
        public override bool CanRunStateless => true;

        public override async Task RunAsync(IServiceProvider services, CancellationToken cancellationToken = default)
        {
            var manager = services.GetRequiredService<InteractiveManager>();

            // retrieve doujin
            var doujin = Interactive?.Doujin;

            if (doujin == null)
            {
                // stateless mode
                if (!DoujinMessage.TryParseDoujinIdFromMessage(Message, out var id))
                    return;

                doujin = await services.GetRequiredService<IDatabase>()
                    .GetDoujinAsync(id.source, id.id, cancellationToken);

                if (doujin == null)
                    return;
            }

            // send download interactive
            await manager.SendInteractiveAsync(new DownloadMessage(doujin), Context, cancellationToken);
        }
    }
}