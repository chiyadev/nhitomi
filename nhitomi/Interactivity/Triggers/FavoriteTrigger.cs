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

                bool added;

                Collection collection;

                do
                {
                    collection = await _database.GetCollectionAsync(
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

                    var existingRef = collection.Doujins.FirstOrDefault(x => x.DoujinId == doujin.Id);

                    if (existingRef == null)
                    {
                        // add to favorites collection
                        collection.Doujins.Add(new CollectionRef
                        {
                            DoujinId = doujin.Id
                        });

                        added = true;
                    }
                    else
                    {
                        // remove from favorites collection
                        collection.Doujins.Remove(existingRef);

                        added = false;
                    }
                }
                while (!await _database.SaveAsync(cancellationToken));

                // reply in DM
                if (added)
                    await Context.ReplyDmAsync("addedToCollection", new {doujin, collection});
                else
                    await Context.ReplyDmAsync("removedFromCollection", new {doujin, collection});

                return true;
            }
        }
    }
}