using System.Threading;
using System.Threading.Tasks;
using Discord;
using nhitomi.Core;
using nhitomi.Discord;

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
                if (!await base.RunAsync(cancellationToken) ||
                    !DoujinMessage.TryParseDoujinIdFromMessage(Context.Message, out var id, out var isFeed))
                    return false;

                var doujin = await _database.GetDoujinAsync(id.source, id.id, cancellationToken);

                if (doujin == null)
                    return false;

                var context = Context as IDiscordContext;

                if (isFeed || Interactive?.Source?.Id != Context.User.Id)
                {
                    // reply in DM if feed message
                    context = new DiscordContextWrapper(context)
                    {
                        Channel = await Context.User.GetOrCreateDMChannelAsync()
                    };
                }

                // send read interactive
                await _interactive.SendInteractiveAsync(new DoujinReadMessage(doujin), context, cancellationToken);

                return true;
            }
        }
    }
}