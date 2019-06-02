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
        public async Task ListCollectionsAsync()
        {
            using (_context.BeginTyping())
            {
                var collections = await _database.GetCollectionsAsync(_context.User.Id);

                await _interactive.SendInteractiveAsync(new CollectionListMessage(collections), _context);
            }
        }

        sealed class CollectionViewMessage : DoujinListMessage<CollectionViewMessage.View>
        {
            readonly ulong _userId;
            readonly string _collectionName;

            public CollectionViewMessage(ulong userId, string collectionName)
            {
                _userId = userId;
                _collectionName = collectionName;
            }

            public class View : DoujinListView
            {
                new CollectionViewMessage Message => (CollectionViewMessage) base.Message;

                readonly IDatabase _db;

                public View(IDatabase db)
                {
                    _db = db;
                }

                protected override Task<Doujin[]> GetValuesAsync(int offset,
                    CancellationToken cancellationToken = default) =>
                    _db.GetCollectionAsync(
                        Message._userId,
                        Message._collectionName,
                        d => d.Skip(offset).Take(10),
                        cancellationToken);
            }
        }

        [Command("view")]
        public async Task ViewAsync(string name)
        {
            using (_context.BeginTyping())
            {
                // check if collection exists first
                var collection = await _database.GetCollectionAsync(_context.User.Id, name);

                if (collection == null)
                {
                    await _context.ReplyAsync("messages.collectionNotFound");
                    return;
                }

                await _interactive.SendInteractiveAsync(new CollectionViewMessage(_context.User.Id, name), _context);
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
                    await _context.ReplyAsync("messages.doujinNotFound");
                    return;
                }

                collection.Doujins.Add(new CollectionRef
                {
                    DoujinId = doujin.Id
                });
            }
            while (!await _database.SaveAsync(cancellationToken));

            await _context.ReplyAsync("messages.addedToCollection");
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

                if (item != null)
                    collection.Doujins.Remove(item);
            }
            while (!await _database.SaveAsync(cancellationToken));

            await _context.ReplyAsync("messages.removedFromCollection");
        }

        [Command("delete")]
        public async Task DeleteAsync(string name)
        {
            using (_context.BeginTyping())
            {
                do
                {
                    var collection = await _database.GetCollectionAsync(_context.User.Id, name);

                    if (collection == null)
                    {
                        await _context.ReplyAsync("messages.collectionNotFound");
                        return;
                    }

                    _database.Remove(collection);
                }
                while (!await _database.SaveAsync());

                await _context.ReplyAsync("messages.collectionDeleted");
            }
        }

        [Command("sort")]
        public async Task SortAsync(string name, CollectionSort sort)
        {
            using (_context.BeginTyping())
            {
                do
                {
                    var collection = await _database.GetCollectionAsync(_context.User.Id, name);

                    if (collection == null)
                    {
                        await _context.ReplyAsync("messages.collectionNotFound");
                        return;
                    }

                    collection.Sort = sort;
                }
                while (!await _database.SaveAsync());

                await _context.ReplyAsync("messages.collectionSorted");
            }
        }
    }
}