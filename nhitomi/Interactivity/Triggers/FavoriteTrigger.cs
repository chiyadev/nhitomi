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
            readonly DiscordService _discord;
            readonly IDatabase _database;

            public Action(DiscordService discord, IDatabase database)
            {
                _discord = discord;
                _database = database;
            }

            const string _collectionName = "Favorites";

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

                do
                {
                    var collection = await _database.GetCollectionAsync(
                        Context.User.Id, _collectionName, cancellationToken);

                    if (collection == null)
                    {
                        // create new collection for favorites
                        collection = new Collection
                        {
                            Name = _collectionName,
                            Owner = new User
                            {
                                Id = Context.User.Id
                            }
                        };

                        _database.Add(collection);
                    }

                    // add to favorites collection
                    collection.Doujins.Add(new DoujinCollection
                    {
                        DoujinId = doujin.Id
                    });
                }
                while (!await _database.SaveAsync(cancellationToken));

                var channel = await _discord.Socket.GetUser(Context.User.Id).GetOrCreateDMChannelAsync();

                //await channel.SendMessageAsync(_formatter.AddedToCollection(_favoritesCollection, doujin));
                return true;
            }
        }
    }
}