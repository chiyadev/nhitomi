using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using nhitomi.Core;

namespace nhitomi.Interactivity.Triggers
{
    public class FavoriteTrigger : ReactionTrigger<IDoujinInteractive>
    {
        public override string Name => "Favorite";
        public override IEmote Emote => new Emoji("\u2764");
        public override bool CanRunStateless => true;

        public override async Task RunAsync(CancellationToken cancellationToken = default)
        {
            // retrieve doujin
            var doujin = Interactive?.Doujin;

            if (doujin == null)
            {
                // stateless mode
                if (!DoujinMessage.TryParseDoujinIdFromMessage(Message, out var id))
                    return;

                doujin = await Services.GetRequiredService<IDatabase>()
                    .GetDoujinAsync(id.source, id.id, cancellationToken);

                if (doujin == null)
                    return;
            }

            var channel = await _discord.Socket.GetUser(reaction.UserId).GetOrCreateDMChannelAsync();

            // add to or remove from favorites
            if (await _database.TryAddToCollectionAsync(reaction.UserId, _favoritesCollection, doujin))
                await channel.SendMessageAsync(_formatter.AddedToCollection(_favoritesCollection, doujin));
        }
    }
}