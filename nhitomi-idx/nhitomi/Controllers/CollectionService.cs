using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nest;
using nhitomi.Database;
using nhitomi.Models;
using nhitomi.Models.Queries;
using OneOf;
using OneOf.Types;
using IElasticClient = nhitomi.Database.IElasticClient;

namespace nhitomi.Controllers
{
    public class CollectionConstraints
    {
        public string OwnerId { get; set; }

        public bool Test(DbCollection collection)
        {
            if (OwnerId != null && Array.IndexOf(collection.OwnerIds, OwnerId) == -1)
                return false;

            return true;
        }
    }

    public interface ICollectionService
    {
        Task<OneOf<DbCollection>> CreateAsync(ObjectType type, CollectionBase model, string userId, CancellationToken cancellationToken = default);
        Task<OneOf<DbCollection, NotFound>> GetAsync(string id, CancellationToken cancellationToken = default);

        Task<SearchResult<DbCollection>> GetUserCollectionsAsync(string id, CancellationToken cancellationToken = default);
        Task<OneOf<IDbEntry<DbCollection>, NotFound>> GetOrCreateUserSpecialCollectionAsync(string userId, ObjectType type, SpecialCollection collection, CancellationToken cancellationToken = default);

        Task<OneOf<DbCollection, NotFound>> UpdateAsync(string id, CollectionBase collection, CollectionConstraints constraints, CancellationToken cancellationToken = default);
        Task<OneOf<DbCollection, NotFound>> SortAsync(string id, string[] items, CollectionConstraints constraints, CancellationToken cancellationToken = default);

        Task<OneOf<DbCollection, NotFound>> AddItemsAsync(string id, string[] items, CollectionInsertPosition position, CollectionConstraints constraints, CancellationToken cancellationToken = default);
        Task<OneOf<DbCollection, NotFound>> RemoveItemsAsync(string id, string[] items, CollectionConstraints constraints, CancellationToken cancellationToken = default);

        Task<OneOf<DbCollection, NotFound>> AddOwnerAsync(string id, string userId, CollectionConstraints constraints, CancellationToken cancellationToken = default);
        Task<OneOf<DbCollection, Success, NotFound>> RemoveOwnerAsync(string id, string userId, CollectionConstraints constraints, CancellationToken cancellationToken = default);

        Task<OneOf<Success, NotFound>> DeleteAsync(string id, CollectionConstraints constraints, CancellationToken cancellationToken = default);
    }

    public class CollectionService : ICollectionService
    {
        readonly IServiceProvider _services;
        readonly IElasticClient _client;

        public CollectionService(IServiceProvider services, IElasticClient client)
        {
            _services = services;
            _client   = client;
        }

        public async Task<OneOf<DbCollection>> CreateAsync(ObjectType type, CollectionBase model, string userId, CancellationToken cancellationToken = default)
        {
            var collection = new DbCollection
            {
                Type     = type,
                OwnerIds = new[] { userId },
                Items    = Array.Empty<string>()
            }.ApplyBase(model, _services);

            return await _client.Entry(collection).CreateAsync(cancellationToken);
        }

        public async Task<OneOf<DbCollection, NotFound>> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            var collection = await _client.GetAsync<DbCollection>(id, cancellationToken);

            if (collection == null)
                return new NotFound();

            return collection;
        }

        sealed class UserCollectionQuery : IQueryProcessor<DbCollection>
        {
            readonly string _userId;

            public UserCollectionQuery(string userId)
            {
                _userId = userId;
            }

            public SearchDescriptor<DbCollection> Process(SearchDescriptor<DbCollection> descriptor)
                => descriptor.Take(1000)
                             .MultiQuery(q => q.Filter((FilterQuery<string>) _userId, c => c.OwnerIds));
        }

        public Task<SearchResult<DbCollection>> GetUserCollectionsAsync(string id, CancellationToken cancellationToken = default)
            => _client.SearchAsync(new UserCollectionQuery(id), cancellationToken);

        public async Task<OneOf<IDbEntry<DbCollection>, NotFound>> GetOrCreateUserSpecialCollectionAsync(string userId, ObjectType type, SpecialCollection collection, CancellationToken cancellationToken = default)
        {
            IDbEntry<DbCollection> collectionEntry;

            var userEntry = await _client.GetEntryAsync<DbUser>(userId, cancellationToken);

            do
            {
                if (userEntry.Value == null)
                    return new NotFound();

                userEntry.Value.SpecialCollections ??= new Dictionary<ObjectType, Dictionary<SpecialCollection, string>>();

                // check if user already has collection
                if (userEntry.Value.SpecialCollections.TryGetValue(type, out var col) && col.TryGetValue(collection, out var collectionId))
                {
                    collectionEntry = await _client.GetEntryAsync<DbCollection>(collectionId, cancellationToken);

                    if (collectionEntry.Value != null)
                        return OneOf<IDbEntry<DbCollection>, NotFound>.FromT0(collectionEntry);
                }

                // create collection after updating user
                collectionEntry = _client.Entry(new DbCollection
                {
                    Type     = type,
                    OwnerIds = new[] { userId },
                    Items    = Array.Empty<string>()
                });

                if (userEntry.Value.SpecialCollections.TryGetValue(type, out col))
                    col[collection] = collectionEntry.Id;
                else
                    userEntry.Value.SpecialCollections[type] = new Dictionary<SpecialCollection, string> { [collection] = collectionEntry.Id };
            }
            while (!await userEntry.TryUpdateAsync(cancellationToken));

            await collectionEntry.CreateAsync(cancellationToken);

            return OneOf<IDbEntry<DbCollection>, NotFound>.FromT0(collectionEntry);
        }

        public async Task<OneOf<DbCollection, NotFound>> UpdateAsync(string id, CollectionBase collection, CollectionConstraints constraints, CancellationToken cancellationToken = default)
        {
            var entry = await _client.GetEntryAsync<DbCollection>(id, cancellationToken);

            do
            {
                if (entry.Value == null || !constraints.Test(entry.Value))
                    return new NotFound();

                if (!entry.Value.TryApplyBase(collection, _services))
                    break;
            }
            while (!await entry.TryUpdateAsync(cancellationToken));

            return entry.Value;
        }

        public async Task<OneOf<DbCollection, NotFound>> SortAsync(string id, string[] items, CollectionConstraints constraints, CancellationToken cancellationToken = default)
        {
            // we build an index table and sort the collection using it
            // this allows the collection to be sorted even if the set of items isn't exactly the same
            // e.g. a consumer fetches collection and uploaded a newly sorted array of (the old set of) items, during which another consumer edits the items in the collection.
            var indexes = new Dictionary<string, int>(items.Length);

            foreach (var item in items)
                indexes.TryAdd(item, indexes.Count);

            var entry = await _client.GetEntryAsync<DbCollection>(id, cancellationToken);

            do
            {
                if (entry.Value == null || !constraints.Test(entry.Value))
                    return new NotFound();

                if (entry.Value.Items.Length == 0)
                    break;

                Array.Sort(entry.Value.Items, (a, b) =>
                {
                    if (!indexes.TryGetValue(a, out var x))
                        return indexes.ContainsKey(b) ? 1 : 0;

                    if (!indexes.TryGetValue(b, out var y))
                        return -1;

                    return x - y;
                });
            }
            while (!await entry.TryUpdateAsync(cancellationToken));

            return entry.Value;
        }

        public async Task<OneOf<DbCollection, NotFound>> AddItemsAsync(string id, string[] items, CollectionInsertPosition position, CollectionConstraints constraints, CancellationToken cancellationToken = default)
        {
            var entry = await _client.GetEntryAsync<DbCollection>(id, cancellationToken);

            do
            {
                if (entry.Value == null || !constraints.Test(entry.Value))
                    return new NotFound();

                if (items.Length == 0)
                    break;

                var result = new string[entry.Value.Items.Length + items.Length];
                var set    = new HashSet<string>(result.Length);

                foreach (var item in entry.Value.Items)
                    set.Add(item);

                var count = 0;

                if (position == CollectionInsertPosition.Start)
                    foreach (var item in items)
                    {
                        if (set.Add(item))
                            result[count++] = item;
                    }

                foreach (var item in entry.Value.Items)
                    result[count++] = item;

                if (position == CollectionInsertPosition.End)
                    foreach (var item in items)
                    {
                        if (set.Add(item))
                            result[count++] = item;
                    }

                Array.Resize(ref result, count);

                entry.Value.Items = result;
            }
            while (!await entry.TryUpdateAsync(cancellationToken));

            return entry.Value;
        }

        public async Task<OneOf<DbCollection, NotFound>> RemoveItemsAsync(string id, string[] items, CollectionConstraints constraints, CancellationToken cancellationToken = default)
        {
            var entry = await _client.GetEntryAsync<DbCollection>(id, cancellationToken);

            do
            {
                if (entry.Value == null || !constraints.Test(entry.Value))
                    return new NotFound();

                if (items.Length == 0)
                    break;

                var result = new string[entry.Value.Items.Length];
                var set    = new HashSet<string>(items.Length);

                foreach (var item in items)
                    set.Add(item);

                var count = 0;

                foreach (var item in entry.Value.Items)
                {
                    if (!set.Contains(item))
                        result[count++] = item;
                }

                Array.Resize(ref result, count);

                entry.Value.Items = result;
            }
            while (!await entry.TryUpdateAsync(cancellationToken));

            return entry.Value;
        }

        public async Task<OneOf<DbCollection, NotFound>> AddOwnerAsync(string id, string userId, CollectionConstraints constraints, CancellationToken cancellationToken = default)
        {
            var entry = await _client.GetEntryAsync<DbCollection>(id, cancellationToken);

            do
            {
                if (entry.Value == null || !constraints.Test(entry.Value))
                    return new NotFound();

                entry.Value.OwnerIds = entry.Value.OwnerIds.Append(userId).Distinct().ToArray();
            }
            while (!await entry.TryUpdateAsync(cancellationToken));

            return entry.Value;
        }

        public async Task<OneOf<DbCollection, Success, NotFound>> RemoveOwnerAsync(string id, string userId, CollectionConstraints constraints, CancellationToken cancellationToken = default)
        {
            var entry = await _client.GetEntryAsync<DbCollection>(id, cancellationToken);

            do
            {
                if (entry.Value == null || !constraints.Test(entry.Value))
                    return new NotFound();

                if (!entry.Value.OwnerIds.Contains(userId))
                    return entry.Value;

                entry.Value.OwnerIds = entry.Value.OwnerIds.Where(x => x != userId).ToArray();

                // if no owner, delete collection
                if (entry.Value.OwnerIds.Length == 0)
                {
                    if (await entry.TryDeleteAsync(cancellationToken))
                        return new Success();
                }

                else
                {
                    if (await entry.TryUpdateAsync(cancellationToken))
                        return entry.Value;
                }
            }
            while (true);
        }

        public async Task<OneOf<Success, NotFound>> DeleteAsync(string id, CollectionConstraints constraints, CancellationToken cancellationToken = default)
        {
            var entry = await _client.GetEntryAsync<DbCollection>(id, cancellationToken);

            do
            {
                if (entry.Value == null || !constraints.Test(entry.Value))
                    return new NotFound();
            }
            while (!await entry.TryDeleteAsync(cancellationToken));

            return new Success();
        }
    }
}