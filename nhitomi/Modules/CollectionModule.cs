using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using nhitomi.Core;
using nhitomi.Database;

namespace nhitomi.Modules
{
    [Group("collection")]
    [Alias("c")]
    public class CollectionModule : ModuleBase
    {
        readonly IDatabase _database;
        readonly MessageFormatter _formatter;
        readonly InteractiveManager _interactive;
        readonly ISet<IDoujinClient> _clients;

        public CollectionModule(
            IDatabase database,
            MessageFormatter formatter,
            InteractiveManager interactive,
            ISet<IDoujinClient> clients)
        {
            _database = database;
            _formatter = formatter;
            _interactive = interactive;
            _clients = clients;
        }

        [Command]
        public async Task ListCollectionsAsync()
        {
            using (Context.Channel.EnterTypingState())
            {
                var collectionNames = await _database.GetCollectionsAsync(Context.User.Id);

                await ReplyAsync(embed: _formatter.CreateCollectionListEmbed(collectionNames));
            }
        }

        [Command]
        public async Task ShowAsync(string collectionName)
        {
            DoujinListInteractive interactive;

            using (Context.Channel.EnterTypingState())
            {
                var items = (IEnumerable<CollectionItemInfo>)
                    await _database.GetCollectionAsync(Context.User.Id, collectionName);

                var doujins = items == null
                    ? AsyncEnumerable.Empty<IDoujin>()
                    : AsyncEnumerable.CreateEnumerable(() =>
                    {
                        var enumerator = items.GetEnumerator();
                        IDoujin current = null;

                        return AsyncEnumerable.CreateEnumerator(
                            async token =>
                            {
                                if (!enumerator.MoveNext())
                                    return false;

                                var client = _clients.FindByName(enumerator.Current.Source);
                                if (client == null)
                                    return false;

                                current = await client.GetAsync(enumerator.Current.Id, token);

                                return current != null;
                            },
                            () => current,
                            enumerator.Dispose);
                    });

                interactive = await _interactive.CreateDoujinListInteractiveAsync(doujins, ReplyAsync);
            }

            if (interactive != null)
                await _formatter.AddDoujinTriggersAsync(interactive.Message);
        }

        [Command]
        public async Task AddAsync(string collectionName, string add, string source, string id)
        {
            if (add != nameof(add))
                return;

            var client = _clients.FindByName(source);

            if (client == null)
            {
                await ReplyAsync(_formatter.UnsupportedSource(source));
                return;
            }

            using (Context.Channel.EnterTypingState())
            {
                // get doujin to create collection item from
                var doujin = await client.GetAsync(id);

                if (doujin == null)
                {
                    await ReplyAsync(_formatter.DoujinNotFound(source));
                    return;
                }

                // add to collection
                await _database.AddToCollectionAsync(Context.User.Id, collectionName, doujin);

                await ReplyAsync(_formatter.AddedToCollection(collectionName, doujin));
            }
        }

        [Command]
        public async Task ListOrDeleteAsync(string collectionName, string operation)
        {
            using (Context.Channel.EnterTypingState())
            {
                var items = await _database.GetCollectionAsync(Context.User.Id, collectionName);

                switch (operation)
                {
                    case "list":
                        await ReplyAsync(embed: _formatter.CreateCollectionEmbed(collectionName, items));
                        break;

                    case "delete":
                        // remove doujins in parallel
                        await Task.WhenAll(items.Select(s =>
                            _database.RemoveFromCollectionAsync(Context.User.Id, collectionName, s)));

                        await ReplyAsync(_formatter.CollectionDeleted(collectionName));
                        break;
                }
            }
        }

        [Command]
        public async Task SortAsync(string collectionName, string sort, string attribute)
        {
            if (sort != nameof(sort))
                return;
        }
    }
}
