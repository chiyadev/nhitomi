using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;
using nhitomi.Core;
using nhitomi.Interactivity;
using nhitomi.Localization;

namespace nhitomi.Modules
{
    [Group("collection"), Alias("c")]
    public class CollectionModule : ModuleBase
    {
        readonly IDatabase _database;
        readonly InteractiveManager _interactive;
        readonly ILocalization _localization;

        public CollectionModule(IDatabase database, InteractiveManager interactive, ILocalization localization)
        {
            _database = database;
            _interactive = interactive;
            _localization = localization;
        }

        [Command]
        public async Task ListCollectionsAsync()
        {
            using (Context.Channel.EnterTypingState())
            {
                var collections = await _database.GetCollectionsAsync(Context.User.Id);

                await _interactive.SendInteractiveAsync(new CollectionListMessage(collections), Context);
            }
        }

        [Command]
        public async Task ViewAsync(string name)
        {
            using (Context.Channel.EnterTypingState())
            {
                var doujins = await _database.EnumerateCollectionAsync(Context.User.Id, name, x => x);

                if (doujins == null)
                {
                    await ReplyAsync(_localization[Context]["messages.collectionNotFound"]);
                    return;
                }

                IAsyncEnumerable<Doujin> enumerate(IDatabase db, int offset) => doujins;

                await _interactive.SendInteractiveAsync(new DoujinListMessage(enumerate), Context);
            }
        }

        [Command]
        public async Task AddOrRemoveAsync(string name, string operation, string source, string id)
        {
            switch (operation)
            {
                case "add":
                    using (Context.Channel.EnterTypingState())
                        await AddAsync(name, source, id);
                    break;

                case "remove":
                    using (Context.Channel.EnterTypingState())
                        await RemoveAsync(name, source, id);
                    break;
            }
        }

        async Task AddAsync(string name, string source, string id, CancellationToken cancellationToken = default)
        {
            Doujin doujin;

            do
            {
                var collection = await _database.GetCollectionAsync(Context.User.Id, name, cancellationToken);

                if (collection == null)
                {
                    collection = new Collection
                    {
                        Name = name,
                        Owner = new User
                        {
                            Id = Context.User.Id
                        }
                    };

                    _database.Add(collection);
                }

                doujin = await _database.GetDoujinAsync(source, id, cancellationToken);

                if (doujin == null)
                {
                    await ReplyAsync(_localization[Context]["messages.doujinNotFound"]);
                    return;
                }

                collection.Doujins.Add(new DoujinCollection
                {
                    DoujinId = doujin.Id
                });
            }
            while (!await _database.SaveAsync(cancellationToken));

            await ReplyAsync(_localization[Context]["messages.addedToCollection"]);
        }

        async Task RemoveAsync(string name, string source, string id, CancellationToken cancellationToken = default)
        {
            Doujin doujin;

            do
            {
                var collection = await _database.GetCollectionAsync(Context.User.Id, name, cancellationToken);

                if (collection == null)
                {
                    await ReplyAsync(_localization[Context]["messages.collectionNotFound"]);
                    return;
                }

                doujin = await _database.GetDoujinAsync(source, id, cancellationToken);

                if (doujin == null)
                {
                    await ReplyAsync(_localization[Context]["messages.doujinNotFound"]);
                    return;
                }

                var item = collection.Doujins.FirstOrDefault(x => x.DoujinId == doujin.Id);

                if (item != null)
                    collection.Doujins.Remove(item);
            }
            while (!await _database.SaveAsync(cancellationToken));

            await ReplyAsync(_localization[Context]["messages.removedFromCollection"]);
        }

        [Command]
        public async Task DeleteAsync(string name, string delete)
        {
            if (delete != nameof(delete))
                return;

            using (Context.Channel.EnterTypingState())
            {
                do
                {
                    var collection = await _database.GetCollectionAsync(Context.User.Id, name);

                    if (collection == null)
                    {
                        await ReplyAsync(_localization[Context]["messages.collectionNotFound"]);
                        return;
                    }

                    _database.Remove(collection);
                }
                while (!await _database.SaveAsync());

                await ReplyAsync(_localization[Context]["messages.collectionDeleted"]);
            }
        }

        [Command]
        public async Task SortAsync(string name, string sort, string attribute)
        {
            if (sort != nameof(sort))
                return;

            // parse sort attribute
            if (!Enum.TryParse<CollectionSort>(attribute, true, out var sortValue))
            {
                await ReplyAsync(_localization[Context]["messages.invalidCollectionSort"]);
                return;
            }

            using (Context.Channel.EnterTypingState())
            {
                do
                {
                    var collection = await _database.GetCollectionAsync(Context.User.Id, name);

                    if (collection == null)
                    {
                        await ReplyAsync(_localization[Context]["messages.collectionNotFound"]);
                        return;
                    }

                    collection.Sort = sortValue;
                }
                while (!await _database.SaveAsync());

                await ReplyAsync(_localization[Context]["messages.collectionSorted"]);
            }
        }
    }
}