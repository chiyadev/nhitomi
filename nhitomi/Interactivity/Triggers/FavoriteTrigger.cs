using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using nhitomi.Core;
using nhitomi.Discord;

namespace nhitomi.Interactivity.Triggers
{
    public class FavoriteTrigger : ReactionTrigger<FavoriteTrigger.Action>
    {
        public override string Name => "Favorite";
        public override IEmote Emote => new Emoji("\u2764");
        public override bool CanRunStateless => true;

        public class Action : ActionBase<IDoujinMessage>
        {
            readonly IDatabase _database;

            public Action(IDatabase database)
            {
                _database = database;
            }

            const string _favoritesCollection = "Favorites";

            public override async Task<bool> RunAsync(CancellationToken cancellationToken = default)
            {
                if (!await base.RunAsync(cancellationToken))
                    return false;

                using (Context.BeginTyping())
                {
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

                    do
                    {
                        var collection = await _database.GetCollectionAsync(
                            Context.User.Id, _favoritesCollection, cancellationToken);

                        if (collection == null)
                        {
                            // create new collection for favorites
                            collection = new Collection
                            {
                                Name = _favoritesCollection,
                                OwnerId = Context.User.Id,
                                Doujins = new List<CollectionRef>()
                            };

                            _database.Add(collection);
                        }

                        if (collection.Doujins.Any(x => x.DoujinId == doujin.Id))
                        {
                            await Context.ReplyDmAsync("messages.alreadyInCollection");
                            return true;
                        }

                        // add to favorites collection
                        collection.Doujins.Add(new CollectionRef
                        {
                            DoujinId = doujin.Id
                        });
                    }
                    while (!await _database.SaveAsync(cancellationToken));

                    // try replying in DM if we don't have perms
                    await Context.ReplyDmAsync("messages.addedToCollection");
                }

                return true;
            }
        }
    }
}