using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using nhitomi.Core;
using nhitomi.Discord;
using nhitomi.Discord.Parsing;
using nhitomi.Globalization;
using nhitomi.Interactivity;

namespace nhitomi.Modules
{
    [Module("collection")]
    public class CollectionModule
    {
        readonly IDiscordContext _context;
        readonly IDatabase _database;
        readonly InteractiveManager _interactive;
        readonly ILocalization _localization;

        public CollectionModule(IDiscordContext context, IDatabase database, InteractiveManager interactive,
            ILocalization localization)
        {
            _context = context;
            _database = database;
            _interactive = interactive;
            _localization = localization;
        }

        [Command("list")]
        public async Task ListCollectionsAsync()
        {
            using (_context.Channel.EnterTypingState())
            {
                var collections = await _database.GetCollectionsAsync(_context.User.Id);

                await _interactive.SendInteractiveAsync(new CollectionListMessage(collections), _context);
            }
        }

        [Command("view")]
        public async Task ViewAsync(string name)
        {
            using (_context.Channel.EnterTypingState())
            {
                var doujins = await _database.EnumerateCollectionAsync(_context.User.Id, name, x => x);

                if (doujins == null)
                {
                    await ReplyAsync(_localization[_context]["messages.collectionNotFound"]);
                    return;
                }

                IAsyncEnumerable<Doujin> enumerate(IDatabase db, int offset) => doujins;

                await _interactive.SendInteractiveAsync(new DoujinListMessage(enumerate), _context);
            }
        }

        [Command]
        public async Task AddOrRemoveAsync(string name, string operation, string source, string id)
        {
            switch (operation)
            {
                case "add":
                    using (_context.Channel.EnterTypingState())
                        await AddAsync(name, source, id);
                    break;

                case "remove":
                    using (_context.Channel.EnterTypingState())
                        await RemoveAsync(name, source, id);
                    break;
            }
        }

        [Command("add"), Binding("[name] add [source] [id]")]
        public async Task AddAsync(string name, string source, string id, CancellationToken cancellationToken = default)
        {
            do
            {
                var collection = await _database.GetCollectionAsync(_context.User.Id, name, cancellationToken);

                if (collection == null)
                {
                    collection = new Collection
                    {
                        Name = name,
                        Owner = new User
                        {
                            Id = _context.User.Id
                        }
                    };

                    _database.Add(collection);
                }

                var doujin = await _database.GetDoujinAsync(source, id, cancellationToken);

                if (doujin == null)
                {
                    await ReplyAsync(_localization[_context]["messages.doujinNotFound"]);
                    return;
                }

                collection.Doujins.Add(new DoujinCollection
                {
                    DoujinId = doujin.Id
                });
            }
            while (!await _database.SaveAsync(cancellationToken));

            await ReplyAsync(_localization[_context]["messages.addedToCollection"]);
        }

        [Command("remove"), Binding("[name] remove [source] [id]")]
        public async Task RemoveAsync(string name, string source, string id,
            CancellationToken cancellationToken = default)
        {
            Doujin doujin;

            do
            {
                var collection = await _database.GetCollectionAsync(_context.User.Id, name, cancellationToken);

                if (collection == null)
                {
                    await ReplyAsync(_localization[_context]["messages.collectionNotFound"]);
                    return;
                }

                doujin = await _database.GetDoujinAsync(source, id, cancellationToken);

                if (doujin == null)
                {
                    await ReplyAsync(_localization[_context]["messages.doujinNotFound"]);
                    return;
                }

                var item = collection.Doujins.FirstOrDefault(x => x.DoujinId == doujin.Id);

                if (item != null)
                    collection.Doujins.Remove(item);
            }
            while (!await _database.SaveAsync(cancellationToken));

            await ReplyAsync(_localization[_context]["messages.removedFromCollection"]);
        }

        [Command("delete")]
        public async Task DeleteAsync(string name)
        {
            using (_context.Channel.EnterTypingState())
            {
                do
                {
                    var collection = await _database.GetCollectionAsync(_context.User.Id, name);

                    if (collection == null)
                    {
                        await ReplyAsync(_localization[_context]["messages.collectionNotFound"]);
                        return;
                    }

                    _database.Remove(collection);
                }
                while (!await _database.SaveAsync());

                await ReplyAsync(_localization[_context]["messages.collectionDeleted"]);
            }
        }

        [Command("sort")]
        public async Task SortAsync(string name, CollectionSort sort)
        {
            using (_context.Channel.EnterTypingState())
            {
                do
                {
                    var collection = await _database.GetCollectionAsync(_context.User.Id, name);

                    if (collection == null)
                    {
                        await ReplyAsync(_localization[_context]["messages.collectionNotFound"]);
                        return;
                    }

                    collection.Sort = sort;
                }
                while (!await _database.SaveAsync());

                await ReplyAsync(_localization[_context]["messages.collectionSorted"]);
            }
        }
    }
}