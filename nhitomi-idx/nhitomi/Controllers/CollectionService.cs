using System;
using System.Collections.Generic;
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
    public interface ICollectionService
    {
        Task<OneOf<DbCollection, NotFound>> GetAsync(ObjectType type, string id, CancellationToken cancellationToken = default);
        Task<SearchResult<DbCollection>> GetUserCollectionsAsync(ObjectType type, string id, bool publicOnly, CancellationToken cancellationToken = default);

        Task<OneOf<DbCollection, NotFound>> UpdateAsync(ObjectType type, string id, CollectionBase collection, CancellationToken cancellationToken = default);
        Task<OneOf<DbCollection, NotFound>> SortAsync(ObjectType type, string id, string[] items, CancellationToken cancellationToken = default);

        Task<OneOf<DbCollection, NotFound>> AddItemsAsync(ObjectType type, string id, string[] items, CollectionInsertPosition position, CancellationToken cancellationToken = default);
        Task<OneOf<DbCollection, NotFound>> RemoveItemsAsync(ObjectType type, string id, string[] items, CancellationToken cancellationToken = default);

        Task<OneOf<Success, NotFound>> DeleteAsync(ObjectType type, string id, CancellationToken cancellationToken = default);
    }

    public class CollectionService : ICollectionService
    {
        readonly IElasticClient _client;

        public CollectionService(IElasticClient client)
        {
            _client = client;
        }

        public async Task<OneOf<DbCollection, NotFound>> GetAsync(ObjectType type, string id, CancellationToken cancellationToken = default)
        {
            var collection = await _client.GetAsync<DbCollection>(id, cancellationToken);

            if (collection?.Type != type)
                return new NotFound();

            return collection;
        }

        sealed class UserCollectionQuery : IQueryProcessor<DbCollection>
        {
            readonly string _userId;
            readonly bool _publicOnly;

            public UserCollectionQuery(string userId, bool publicOnly)
            {
                _userId     = userId;
                _publicOnly = publicOnly;
            }

            public SearchDescriptor<DbCollection> Process(SearchDescriptor<DbCollection> descriptor)
                => descriptor.Take(1000)
                             .MultiQuery(q =>
                              {
                                  q = q.Filter((FilterQuery<string>) _userId, c => c.OwnerIds);

                                  if (_publicOnly)
                                      q = q.Filter((FilterQuery<bool>) true, c => c.IsPublic);

                                  return q;
                              });
        }

        public Task<SearchResult<DbCollection>> GetUserCollectionsAsync(ObjectType type, string id, bool publicOnly, CancellationToken cancellationToken = default)
            => _client.SearchAsync(new UserCollectionQuery(id, publicOnly), cancellationToken);

        public async Task<OneOf<DbCollection, NotFound>> UpdateAsync(ObjectType type, string id, CollectionBase collection, CancellationToken cancellationToken = default)
        {
            var entry = await _client.GetEntryAsync<DbCollection>(id, cancellationToken);

            do
            {
                if (entry.Value?.Type != type)
                    return new NotFound();

                if (!entry.Value.TryApplyBase(collection))
                    break;
            }
            while (!await entry.TryUpdateAsync(cancellationToken));

            return entry.Value;
        }

        public async Task<OneOf<DbCollection, NotFound>> SortAsync(ObjectType type, string id, string[] items, CancellationToken cancellationToken = default)
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
                if (entry.Value?.Type != type)
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

        public async Task<OneOf<DbCollection, NotFound>> AddItemsAsync(ObjectType type, string id, string[] items, CollectionInsertPosition position, CancellationToken cancellationToken = default)
        {
            var entry = await _client.GetEntryAsync<DbCollection>(id, cancellationToken);

            do
            {
                if (entry.Value?.Type != type)
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

        public async Task<OneOf<DbCollection, NotFound>> RemoveItemsAsync(ObjectType type, string id, string[] items, CancellationToken cancellationToken = default)
        {
            var entry = await _client.GetEntryAsync<DbCollection>(id, cancellationToken);

            do
            {
                if (entry.Value?.Type != type)
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

        public async Task<OneOf<Success, NotFound>> DeleteAsync(ObjectType type, string id, CancellationToken cancellationToken = default)
        {
            var entry = await _client.GetEntryAsync<DbCollection>(id, cancellationToken);

            do
            {
                if (entry.Value?.Type != type)
                    return new NotFound();
            }
            while (!await entry.TryDeleteAsync(cancellationToken));

            return new Success();
        }
    }
}