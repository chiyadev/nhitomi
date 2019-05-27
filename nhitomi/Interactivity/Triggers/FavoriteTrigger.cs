using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using nhitomi.Core;
using nhitomi.Discord;

namespace nhitomi.Interactivity.Triggers
{
    public class FavoriteTrigger : ReactionTrigger<IDoujinInteractive>
    {
        const string _collectionName = "Favorites";

        public override string Name => "Favorite";
        public override IEmote Emote => new Emoji("\u2764");
        public override bool CanRunStateless => true;

        public override async Task RunAsync(IServiceProvider services, CancellationToken cancellationToken = default)
        {
            var discord = services.GetRequiredService<DiscordService>();
            var database = services.GetRequiredService<IDatabase>();

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

            do
            {
                var collection = await database.GetCollectionAsync(Context.User.Id, _collectionName, cancellationToken);

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

                    database.Add(collection);
                }

                // add to favorites collection
                collection.Doujins.Add(new DoujinCollection
                {
                    DoujinId = doujin.Id
                });
            }
            while (!await database.SaveAsync(cancellationToken));

            var channel = await discord.Socket.GetUser(Context.User.Id).GetOrCreateDMChannelAsync();

            //await channel.SendMessageAsync(_formatter.AddedToCollection(_favoritesCollection, doujin));
        }
    }
}