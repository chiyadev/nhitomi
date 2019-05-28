using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using nhitomi.Core;
using nhitomi.Discord.Parsing;
using nhitomi.Globalization;
using nhitomi.Interactivity;

namespace nhitomi.Modules
{
    [Module("collection")]
    public class CollectionModule
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

        [Command("list")]
        public async Task ListCollectionsAsync()
        {
            using (Context.Channel.EnterTypingState())
            {
                var collections = await _database.GetCollectionsAsync(Context.User.Id);

                await _interactive.SendInteractiveAsync(new CollectionListMessage(collections), Context);
            }
        }

        [Command("view")]
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

        [Command("add"), Binding("[name] add [source] [id]")]
        public async Task AddAsync(string name, string source, string id, CancellationToken cancellationToken = default)
        {
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

                var doujin = await _database.GetDoujinAsync(source, id, cancellationToken);

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

        [Command("remove"), Binding("[name] remove [source] [id]")]
        public async Task RemoveAsync(string name, string source, string id,
            CancellationToken cancellationToken = default)
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

        [Command("delete")]
        public async Task DeleteAsync(string name)
        {
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

        [Command("sort")]
        public async Task SortAsync(string name, CollectionSort sort)
        {
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

                    collection.Sort = sort;
                }
                while (!await _database.SaveAsync());

                await ReplyAsync(_localization[Context]["messages.collectionSorted"]);
            }
        }
    }
}