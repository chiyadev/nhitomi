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

        public CollectionModule(IMessageContext context,
                                IDatabase database,
                                InteractiveManager interactive)
        {
            _context     = context;
            _database    = database;
            _interactive = interactive;
        }

        static string FixCollectionName(string name)
        {
            switch (name.ToLowerInvariant())
            {
                case "favs": return "favorites";

                default: return name;
            }
        }

        [Command("list")]
        public Task ListAsync(CancellationToken cancellationToken = default) =>
            _interactive.SendInteractiveAsync(new CollectionListMessage(_context.User.Id), _context, cancellationToken);

        [Command("view", BindName = false), Binding("[name]")]
        public async Task ViewAsync(string name,
                                    CancellationToken cancellationToken = default)
        {
            name = FixCollectionName(name);

            // check if collection exists first
            var collection = await _database.GetCollectionAsync(_context.User.Id, name, cancellationToken);

            if (collection == null)
            {
                await _context.ReplyAsync("collectionNotFound", new { name });
                return;
            }

            await _interactive.SendInteractiveAsync(
                new CollectionMessage(_context.User.Id, name),
                _context,
                cancellationToken);
        }

        [Command("add", BindName = false), Binding("[name] add|a [source] [id]")]
        public async Task AddAsync(string name,
                                   string source,
                                   string id,
                                   CancellationToken cancellationToken = default)
        {
            name = FixCollectionName(name);

            Doujin     doujin;
            Collection collection;

            do
            {
                collection = await _database.GetCollectionAsync(_context.User.Id, name, cancellationToken);

                if (collection == null)
                {
                    collection = new Collection
                    {
                        Name    = name,
                        OwnerId = _context.User.Id,
                        Doujins = new List<CollectionRef>()
                    };

                    _database.Add(collection);
                }

                doujin = await _database.GetDoujinAsync(source, id, cancellationToken);

                if (doujin == null)
                {
                    await _context.ReplyAsync("doujinNotFound");
                    return;
                }

                if (collection.Doujins.Any(x => x.DoujinId == doujin.Id))
                {
                    await _context.ReplyAsync("alreadyInCollection", new { doujin, collection });
                    return;
                }

                collection.Doujins.Add(new CollectionRef
                {
                    DoujinId = doujin.Id
                });
            }
            while (!await _database.SaveAsync(cancellationToken));

            await _context.ReplyAsync("addedToCollection", new { doujin, collection });
        }

        [Command("add", BindName = false), Binding("[name] add|a [url+]")]
        public Task AddAsync(string name,
                             string url,
                             CancellationToken cancellationToken = default)
        {
            var (source, id) = GalleryUtility.Parse(url);

            return AddAsync(name, source, id, cancellationToken);
        }

        [Command("remove", BindName = false), Binding("[name] remove|r [source] [id]")]
        public async Task RemoveAsync(string name,
                                      string source,
                                      string id,
                                      CancellationToken cancellationToken = default)
        {
            name = FixCollectionName(name);

            Doujin     doujin;
            Collection collection;

            do
            {
                collection = await _database.GetCollectionAsync(_context.User.Id, name, cancellationToken);

                if (collection == null)
                {
                    await _context.ReplyAsync("collectionNotFound", new { name });
                    return;
                }

                doujin = await _database.GetDoujinAsync(source, id, cancellationToken);

                if (doujin == null)
                {
                    await _context.ReplyAsync("doujinNotFound");
                    return;
                }

                var item = collection.Doujins.FirstOrDefault(x => x.DoujinId == doujin.Id);

                if (item == null)
                {
                    await _context.ReplyAsync("notInCollection", new { doujin, collection });
                    return;
                }

                collection.Doujins.Remove(item);
            }
            while (!await _database.SaveAsync(cancellationToken));

            await _context.ReplyAsync("removedFromCollection", new { doujin, collection });
        }

        [Command("remove", BindName = false), Binding("[name] remove|r [url+]")]
        public Task RemoveAsync(string name,
                                string url,
                                CancellationToken cancellationToken = default)
        {
            var (source, id) = GalleryUtility.Parse(url);

            return RemoveAsync(name, source, id, cancellationToken);
        }

        [Command("delete", BindName = false), Binding("[name] delete|d")]
        public async Task DeleteAsync(string name,
                                      CancellationToken cancellationToken = default)
        {
            name = FixCollectionName(name);

            Collection collection;

            do
            {
                collection = await _database.GetCollectionAsync(_context.User.Id, name, cancellationToken);

                if (collection == null)
                {
                    await _context.ReplyAsync("collectionNotFound", new { name });
                    return;
                }

                _database.Remove(collection);
            }
            while (!await _database.SaveAsync(cancellationToken));

            await _context.ReplyAsync("collectionDeleted", new { collection });
        }

        [Command("sort", BindName = false), Binding("[name] sort|s [sort]")]
        public async Task SortAsync(string name,
                                    CollectionSort sort,
                                    CancellationToken cancellationToken = default)
        {
            name = FixCollectionName(name);

            Collection collection;

            do
            {
                collection = await _database.GetCollectionAsync(_context.User.Id, name, cancellationToken);

                if (collection == null)
                {
                    await _context.ReplyAsync("collectionNotFound", new { name });
                    return;
                }

                collection.Sort = sort;
            }
            while (!await _database.SaveAsync(cancellationToken));

            await _context.ReplyAsync("collectionSorted", new { collection, attribute = sort });
        }
    }
}