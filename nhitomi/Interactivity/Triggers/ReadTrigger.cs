using System.Threading;
using System.Threading.Tasks;
using Discord;
using nhitomi.Core;

namespace nhitomi.Interactivity.Triggers
{
    public class ReadTrigger : ReactionTrigger<ReadTrigger.Action>
    {
        public override string Name => "Read";
        public override IEmote Emote => new Emoji("\uD83D\uDCD6");
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

                // send read interactive
                await _interactive.SendInteractiveAsync(new DoujinReadMessage(doujin), Context, cancellationToken);

                return true;
            }
        }
    }
}