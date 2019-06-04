using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using nhitomi.Core;
using nhitomi.Discord;
using nhitomi.Discord.Parsing;
using nhitomi.Interactivity;

namespace nhitomi.Modules
{
    [Module("collection")]
    public class CollectionModule
    {
        readonly IMessageContext _context;
        readonly IDatabase _database;
        readonly InteractiveManager _interactive;

        public CollectionModule(IMessageContext context, IDatabase database, InteractiveManager interactive)
        {
            _context = context;
            _database = database;
            _interactive = interactive;
        }

        [Command("list")]
        public Task ListAsync() =>
            _interactive.SendInteractiveAsync(new CollectionListMessage(_context.User.Id), _context);

        [Command("view", BindName = false), Binding("[name]")]
        public async Task ViewAsync(string name)
        {
            // check if collection exists first
            var collection = await _database.GetCollectionAsync(_context.User.Id, name);

            if (collection == null)
            {
                await _context.ReplyAsync("messages.collectionNotFound");
                return;
            }

            await _interactive.SendInteractiveAsync(new CollectionDoujinListMessage(_context.User.Id, name), _context);
        }

        [Command("add", BindName = false), Binding("[name] add|a [source] [id]")]
        public async Task AddAsync(string name, string source, string id, CancellationToken cancellationToken = default)
        {
            Doujin doujin;
            Collection collection;

            do
            {
                collection = await _database.GetCollectionAsync(_context.User.Id, name, cancellationToken);

                if (collection == null)
                {
                    collection = new Collection
                    {
                        Id = Guid.NewGuid(),
                        Name = name,
                        OwnerId = _context.User.Id,
                        Doujins = new List<CollectionRef>()
                    };

                    _database.Add(collection);
                }

                doujin = await _database.GetDoujinAsync(source, id, cancellationToken);

                if (doujin == null)
                {
                    await _context.ReplyAsync("messages.doujinNotFound");
                    return;
                }

                if (collection.Doujins.Any(x => x.DoujinId == doujin.Id))
                {
                    await _context.ReplyAsync("messages.alreadyInCollection", new {doujin, collection});
                    return;
                }

                collection.Doujins.Add(new CollectionRef
                {
                    DoujinId = doujin.Id
                });
            }
            while (!await _database.SaveAsync(cancellationToken));

            await _context.ReplyAsync("messages.addedToCollection", new {doujin, collection});
        }

        [Command("remove", BindName = false), Binding("[name] remove|r [source] [id]")]
        public async Task RemoveAsync(string name, string source, string id,
            CancellationToken cancellationToken = default)
        {
            Doujin doujin;
            Collection collection;

            do
            {
                collection = await _database.GetCollectionAsync(_context.User.Id, name, cancellationToken);

                if (collection == null)
                {
                    await _context.ReplyAsync("messages.collectionNotFound");
                    return;
                }

                doujin = await _database.GetDoujinAsync(source, id, cancellationToken);

                if (doujin == null)
                {
                    await _context.ReplyAsync("messages.doujinNotFound");
                    return;
                }

                var item = collection.Doujins.FirstOrDefault(x => x.DoujinId == doujin.Id);

                if (item == null)
                {
                    await _context.ReplyAsync("messages.notInCollection", new {doujin, collection});
                    return;
                }

                collection.Doujins.Remove(item);
            }
            while (!await _database.SaveAsync(cancellationToken));

            await _context.ReplyAsync("messages.removedFromCollection", new {doujin, collection});
        }

        [Command("delete", BindName = false), Binding("[name] delete|d")]
        public async Task DeleteAsync(string name)
        {
            Collection collection;

            do
            {
                collection = await _database.GetCollectionAsync(_context.User.Id, name);

                if (collection == null)
                {
                    await _context.ReplyAsync("messages.collectionNotFound");
                    return;
                }

                _database.Remove(collection);
            }
            while (!await _database.SaveAsync());

            await _context.ReplyAsync("messages.collectionDeleted", new {collection});
        }

        [Command("sort", BindName = false), Binding("[name] sort|s [sort]")]
        public async Task SortAsync(string name, CollectionSort sort)
        {
            Collection collection;

            do
            {
                collection = await _database.GetCollectionAsync(_context.User.Id, name);

                if (collection == null)
                {
                    await _context.ReplyAsync("messages.collectionNotFound");
                    return;
                }

                collection.Sort = sort;
            }
            while (!await _database.SaveAsync());

            await _context.ReplyAsync("messages.collectionSorted", new {collection, attribute = sort});
        }
    }
}