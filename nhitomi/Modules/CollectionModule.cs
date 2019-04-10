using System;
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

                if (items == null)
                {
                    await ReplyAsync(_formatter.CollectionNotFound);
                    return;
                }

                var doujins = AsyncEnumerable.CreateEnumerable(() =>
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
        public async Task AddOrRemoveAsync(string collectionName, string operation, string source, string id)
        {
            if (operation != "add" && operation != "remove")
                return;

            var client = _clients.FindByName(source);

            if (client == null)
            {
                await ReplyAsync(_formatter.UnsupportedSource(source));
                return;
            }

            switch (operation)
            {
                case "add":
                    await AddAsync(collectionName, client, source, id);
                    break;

                case "remove":
                    await RemoveAsync(collectionName, source, id);
                    break;
            }
        }

        async Task AddAsync(string collectionName, IDoujinClient client, string source, string id)
        {
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
                if (await _database.TryAddToCollectionAsync(Context.User.Id, collectionName, doujin))
                    await ReplyAsync(_formatter.AddedToCollection(collectionName, doujin));
                else
                    await ReplyAsync(_formatter.AlreadyInCollection(collectionName, doujin));
            }
        }

        async Task RemoveAsync(string collectionName, string source, string id)
        {
            using (Context.Channel.EnterTypingState())
            {
                var item = new CollectionItemInfo
                {
                    Source = source,
                    Id = id
                };

                // remove from collection
                if (await _database.TryRemoveFromCollectionAsync(Context.User.Id, collectionName, item))
                    await ReplyAsync(_formatter.RemovedFromCollection(collectionName, item));
                else
                    await ReplyAsync(_formatter.NotInCollection(collectionName, item));
            }
        }

        [Command]
        public async Task ListOrDeleteAsync(string collectionName, string operation)
        {
            switch (operation)
            {
                case "list":
                    await ListAsync(collectionName);
                    break;

                case "delete":
                    await DeleteAsync(collectionName);
                    break;
            }
        }

        async Task ListAsync(string collectionName)
        {
            CollectionInteractive interactive;

            using (Context.Channel.EnterTypingState())
            {
                var items = await _database.GetCollectionAsync(Context.User.Id, collectionName);

                if (items == null)
                {
                    await ReplyAsync(_formatter.CollectionNotFound);
                    return;
                }

                interactive =
                    await _interactive.CreateCollectionInteractiveAsync(collectionName, items, ReplyAsync);
            }

            if (interactive != null)
                await _formatter.AddCollectionTriggersAsync(interactive.Message);
        }

        async Task DeleteAsync(string collectionName)
        {
            using (Context.Channel.EnterTypingState())
            {
                if (await _database.TryDeleteCollectionAsync(Context.User.Id, collectionName))
                    await ReplyAsync(_formatter.CollectionDeleted(collectionName));
                else
                    await ReplyAsync(_formatter.CollectionNotFound);
            }
        }

        [Command]
        public async Task SortAsync(string collectionName, string sort, string attribute)
        {
            if (sort != nameof(sort))
                return;

            // parse sort attribute
            if (!Enum.TryParse<CollectionSortAttribute>(attribute, true, out var attributeValue))
            {
                await ReplyAsync(_formatter.InvalidSortAttribute(attribute));
                return;
            }

            using (Context.Channel.EnterTypingState())
            {
                await _database.SetCollectionSortAsync(Context.User.Id, collectionName, attributeValue);

                await ReplyAsync(_formatter.SortAttributeUpdated(attributeValue));
            }
        }
    }
}