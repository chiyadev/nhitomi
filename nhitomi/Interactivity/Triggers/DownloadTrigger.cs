using System.Threading;
using System.Threading.Tasks;
using Discord;
using nhitomi.Core;

namespace nhitomi.Interactivity.Triggers
{
    public class DownloadTrigger : ReactionTrigger<DownloadTrigger.Action>
    {
        public override string Name => "Download";
        public override IEmote Emote => new Emoji("\uD83D\uDCBE");
        public override bool CanRunStateless => true;

        public class Action : ActionBase<IDoujinMessage>
        {
            readonly IDatabase _database;
            readonly InteractiveManager _interactive;

            public Action(IDatabase database, InteractiveManager interactive)
            {
                _database = database;
                _interactive = interactive;
            }

            public override async Task<bool> RunAsync(CancellationToken cancellationToken = default)
            {
                if (!await base.RunAsync(cancellationToken))
                    return false;

                // retrieve doujin
                var doujin = Interactive?.Doujin;

                if (doujin == null)
                {
                    // stateless mode
                    if (!DoujinMessage.TryParseDoujinIdFromMessage(Context.Message, out var id))
                        return false;

                    doujin = await _database.GetDoujinAsync(id.source, id.id, cancellationToken);

                    if (doujin == null)
                        return false;
                }

                // send download interactive
                await _interactive.SendInteractiveAsync(new DownloadMessage(doujin), Context, cancellationToken);

                return true;
            }
        }
    }
}