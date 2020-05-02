using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ChiyaFlake;
using Elasticsearch.Net;
using MessagePack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using nhitomi.Models;
using nhitomi.Models.Queries;
using StackExchange.Redis;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace nhitomi.Database
{
    public class ElasticOptions
    {
        /// <summary>
        /// Elasticsearch endpoint.
        /// </summary>
        public string Endpoint { get; set; } = "localhost:9200";

        /// <summary>
        /// Prefix to use when creating indexes.
        /// </summary>
        public string IndexPrefix { get; set; } = "nhitomi-";

        /// <summary>
        /// Number of shards to configure when creating a new index.
        /// This cannot be changed after creating an index unless reindexing manually.
        /// </summary>
        public int ShardCount { get; set; } = 5;

        /// <summary>
        /// Number of shard replicas to configure when creating a new index.
        /// </summary>
        public int ReplicaCount { get; set; } = 1;

        /// <summary>
        /// Time between Elasticsearch index refreshes.
        /// </summary>
        public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Refresh setting when updating Elasticsearch indexes.
        /// Disabling can cause consistency problems.
        /// </summary>
        public Refresh RequestRefreshOption { get; set; } = Refresh.WaitFor;

        /// <summary>
        /// Prefix to use when caching objects.
        /// </summary>
        public string CachePrefix { get; set; } = "el:";

        /// <summary>
        /// Time to wait when Elasticsearch is being overloaded.
        /// </summary>
        public TimeSpan RateLimitWait { get; set; } = TimeSpan.FromSeconds(3);

        /// <summary>
        /// True to enable dynamic server configuration stored in Elasticsearch.
        /// </summary>
        public bool EnableDynamicConfig { get; set; } = true;

        /// <summary>
        /// Number of items to search per chunk when searching as a stream.
        /// </summary>
        public int StreamSearchChunkSize { get; set; } = 20;
    }

    public interface IQueryProcessor<T> where T : class, IDbObject
    {
        bool Valid => true;

        SearchDescriptor<T> Process(SearchDescriptor<T> descriptor);
    }

    public abstract class QueryProcessorBase<T, TQuery> : IQueryProcessor<T> where T : class, IDbObject where TQuery : Models.Queries.QueryBase
    {
        protected readonly TQuery Query;

        protected QueryProcessorBase(TQuery query)
        {
            Query = query;
        }

        public virtual SearchDescriptor<T> Process(SearchDescriptor<T> descriptor)
            => descriptor.Skip(Query.Offset)
                         .Take(Query.Limit);
    }

    public interface ISuggestProcessor<T, out TResult> : IQueryProcessor<T> where T : class, IDbObject, IDbSupportsAutocomplete where TResult : SuggestResult
    {
        CompletionSuggesterDescriptor<T> Process(CompletionSuggesterDescriptor<T> descriptor);
        TResult CreateResult(IEnumerable<ISuggestOption<T>> options);
    }

    public abstract class SuggestProcessorBase<T, TResult> : ISuggestProcessor<T, TResult> where T : class, IDbObject, IDbSupportsAutocomplete where TResult : SuggestResult
    {
        public bool Valid => Query.Limit > 0; // normal search can have size=0 but completion suggester can't

        protected readonly SuggestQuery Query;

        protected SuggestProcessorBase(SuggestQuery query)
        {
            Query = query;
        }

        public virtual SearchDescriptor<T> Process(SearchDescriptor<T> descriptor)
            => descriptor.Source(false)
                         .Take(0);

        public virtual CompletionSuggesterDescriptor<T> Process(CompletionSuggesterDescriptor<T> descriptor)
        {
            descriptor = descriptor.Size(Query.Limit)
                                   .Prefix(Query.Prefix)
                                   .Field(x => x.Suggest)
                                   .SkipDuplicates();

            if (Query.Fuzzy)
                descriptor = descriptor.Fuzzy(f => f.Transpositions()
                                                    .UnicodeAware());

            return descriptor;
        }

        public abstract TResult CreateResult(IEnumerable<ISuggestOption<T>> options);
    }

    /// <summary>
    /// Represents an entry in the database.
    /// Entry objects can be used to implement optimistic concurrency.
    /// </summary>
    public interface IDbEntry<T> where T : class, IDbObject
    {
        /// <summary>
        /// Entry ID.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Entry value.
        /// </summary>
        T Value { get; set; }

        /// <summary>
        /// Creates an entry in the database, throwing <see cref="ConcurrencyException"/> if it already exists.
        /// </summary>
        Task<T> CreateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempts <see cref="CreateAsync"/> and returns true.
        /// If concurrency test fails, entry will be refreshed.
        /// </summary>
        async Task<bool> TryCreateAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await CreateAsync(cancellationToken);
                return true;
            }
            catch (ConcurrencyException)
            {
                await RefreshAsync(cancellationToken);
                return false;
            }
        }

        /// <summary>
        /// Updates an entry in the database, throwing <see cref="ConcurrencyException"/> if it was updated by another process after this entry was retrieved.
        /// </summary>
        /// <remarks>
        /// If the entry does not exist, it will be created.
        /// This has the same effect as an "upsert".
        /// </remarks>
        Task<T> UpdateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempts <see cref="UpdateAsync"/> and returns true.
        /// If concurrency test fails, entry will be refreshed.
        /// </summary>
        async Task<bool> TryUpdateAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await UpdateAsync(cancellationToken);
                return true;
            }
            catch (ConcurrencyException)
            {
                await RefreshAsync(cancellationToken);
                return false;
            }
        }

        /// <summary>
        /// Deletes an entry in the database, throwing <see cref="ConcurrencyException"/> if it was updated by another process after this entry was retrieved.
        /// Value will become null after successful deletion.
        /// </summary>
        Task DeleteAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempts <see cref="DeleteAsync"/> and returns true.
        /// If concurrency test fails, entry will be refreshed.
        /// </summary>
        async Task<bool> TryDeleteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await DeleteAsync(cancellationToken);
                return true;
            }
            catch (ConcurrencyException)
            {
                await RefreshAsync(cancellationToken);
                return false;
            }
        }

        /// <summary>
        /// Refreshes cached value, useful when <see cref="ConcurrencyException"/> occurs.
        /// </summary>
        Task<T> RefreshAsync(CancellationToken cancellationToken = default);
    }

    public interface IElasticClient : IDisposable
    {
        Task InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a wrapper object of the specified type with the given ID.
        /// </summary>
        IDbEntry<T> Entry<T>(string id) where T : class, IDbObject;

        /// <summary>
        /// Wraps the given object in <see cref="IDbEntry{T}"/>.
        /// <see cref="IHasId.Id"/> will be generated automatically if null.
        /// </summary>
        /// <remarks>
        /// This method is useful when creating a new object in the database.
        /// Operations on the object will bypass concurrency checks because concurrency information had not been loaded (hence it is not recommended).
        /// </remarks>
        IDbEntry<T> Entry<T>(T value) where T : class, IDbObject => Entry<T>(value.Id ?? Snowflake.New).Chain(x => x.Value = value);

        /// <summary>
        /// Retrieves an object from the database.
        /// This is equivalent to calling <see cref="GetEntryAsync{T}"/> and reading its <see cref="IDbEntry{T}.Value"/>.
        /// </summary>
        async Task<T> GetAsync<T>(string id, CancellationToken cancellationToken = default) where T : class, IDbObject => (await GetEntryAsync<T>(id, cancellationToken)).Value;

        /// <summary>
        /// Retrieves an object from the database and wraps it in <see cref="IDbEntry{T}"/>.
        /// </summary>
        /// <returns>never null</returns>
        Task<IDbEntry<T>> GetEntryAsync<T>(string id, CancellationToken cancellationToken = default) where T : class, IDbObject;

        /// <summary>
        /// Searches for objects from the database.
        /// This is equivalent to calling <see cref="SearchEntriesAsync{T}"/> and reading <see cref="IDbEntry{T}.Value"/>s.
        /// </summary>
        async Task<SearchResult<T>> SearchAsync<T>(IQueryProcessor<T> processor, CancellationToken cancellationToken = default) where T : class, IDbObject => (await SearchEntriesAsync(processor, cancellationToken)).Project(x => x.Value);

        /// <summary>
        /// Searches for objects from the database as an asynchronous stream.
        /// This is equivalent to calling <see cref="SearchEntriesStreamAsync{T}"/> and reading <see cref="IDbEntry{T}.Value"/>s.
        /// </summary>
        IAsyncEnumerable<T> SearchStreamAsync<T>(IQueryProcessor<T> processor, CancellationToken cancellationToken = default) where T : class, IDbObject => SearchEntriesStreamAsync(processor, cancellationToken).Select(x => x.Value);

        /// <summary>
        /// Searches for objects from the database and wraps them in <see cref="IDbEntry{T}"/>.
        /// </summary>
        Task<SearchResult<IDbEntry<T>>> SearchEntriesAsync<T>(IQueryProcessor<T> processor, CancellationToken cancellationToken = default) where T : class, IDbObject;

        /// <summary>
        /// Searches for objects from the database as an asynchronous stream, wrapped in <see cref="IDbEntry{T}"/>.
        /// </summary>
        IAsyncEnumerable<IDbEntry<T>> SearchEntriesStreamAsync<T>(IQueryProcessor<T> processor, CancellationToken cancellationToken = default) where T : class, IDbObject;

        Task<TResult> SuggestAsync<T, TResult>(ISuggestProcessor<T, TResult> processor, CancellationToken cancellationToken = default) where T : class, IDbObject, IDbSupportsAutocomplete where TResult : SuggestResult;

        /// <summary>
        /// Retrieves the number of objects of the specified type in the database.
        /// </summary>
        Task<int> CountAsync<T>(CancellationToken cancellationToken = default) where T : class, IDbObject;

        /// <summary>
        /// Deletes all indexes created by this client. This is useful for unit testing.
        /// </summary>
        Task ResetAsync(CancellationToken cancellationToken = default);
    }

    public class ElasticClient : IElasticClient
    {
        readonly Nest.ElasticClient _client;

        readonly IOptionsMonitor<ElasticOptions> _options;
        readonly IRedisClient _redis;
        readonly IResourceLocker _locker;
        readonly ILogger<Nest.ElasticClient> _logger;

        public ElasticClient(IOptionsMonitor<ElasticOptions> options, IRedisClient redis, IResourceLocker locker, ILogger<Nest.ElasticClient> logger)
        {
            var opts = options.CurrentValue;

            _options = options;
            _redis   = redis;
            _locker  = locker;
            _logger  = logger;

            if (opts.Endpoint == null)
                throw new ArgumentException("Elasticsearch endpoint is not configured.");

            var pool = new SingleNodeConnectionPool(new Uri($"http://{opts.Endpoint}"));

            _client = new Nest.ElasticClient(new ConnectionSettings(pool).DisableDirectStreaming().ThrowExceptions(false));
        }

        public Task InitializeAsync(CancellationToken cancellationToken)
        {
            _logger.LogWarning($"Connecting to Elasticsearch endpoint: {_options.CurrentValue.Endpoint}");

            return Task.CompletedTask;
        }

        async Task<T> Request<T>(Func<Nest.ElasticClient, Task<T>> request) where T : IResponse
        {
            var response = await request(_client);

            if (!response.IsValid)
            {
                switch (response.ServerError?.Status ?? response.ApiCall?.HttpStatusCode)
                {
                    // not found
                    case 404:
                        return response;

                    // concurrency conflict
                    case 409:
                        throw new ConcurrencyException(response.OriginalException);

                    // rate limiting (retry)
                    case 429:
                        await Task.Delay(_options.CurrentValue.RateLimitWait);

                        return await Request(request);
                }

                _logger.LogDebug(response.DebugInformation?.Trim());

                if (response.OriginalException != null)
                    throw response.OriginalException;
            }

            return response;
        }

#region Index management

        readonly struct IndexInfo
        {
            public readonly string Name;
            public readonly string IndexName;
            public readonly string CachePrefix;

            public IndexInfo(Type type, string indexPrefix, string cachePrefix)
            {
                Name = (type.GetCustomAttribute<ElasticsearchTypeAttribute>()?.RelationName ?? type.Name).ToLowerInvariant();

                IndexName   = indexPrefix + Name;
                CachePrefix = cachePrefix + Name + ":";
            }

            public override string ToString() => IndexName;
        }

        readonly ConcurrentDictionary<Type, IndexInfo> _indexes = new ConcurrentDictionary<Type, IndexInfo>();

        async ValueTask<IndexInfo> GetIndexAsync<T>(CancellationToken cancellationToken = default) where T : class
        {
            var options = _options.CurrentValue;

            var type = typeof(T);

            if (_indexes.TryGetValue(type, out var index))
                return index;

            index = new IndexInfo(type, options.IndexPrefix, options.CachePrefix);

            var measure = new MeasureContext();

            await using (await _locker.EnterAsync($"elastic:index:{index}", cancellationToken))
            {
                // may have been created while we were waiting
                if (_indexes.TryGetValue(type, out var newIndex))
                    return newIndex;

                var excludeFields = typeof(T).GetProperties()
                                             .Where(p => p.IsDefined(typeof(DbCachedAttribute)))
                                             .ToArray(p => p.GetCustomAttributes().OfType<IPropertyMapping>().First().Name);

                // index mapping update
                if ((await Request(c => c.Indices.ExistsAsync(index.IndexName, null, cancellationToken))).Exists)
                {
                    IPutMappingRequest map(PutMappingDescriptor<T> m)
                        => m.Index(index.IndexName)
                            .AutoMap()
                            .SourceField(s => s.Excludes(excludeFields))
                            .Dynamic(false);

                    IPromise<IDynamicIndexSettings> settings(DynamicIndexSettingsDescriptor s)
                        => s.NumberOfReplicas(options.ReplicaCount)
                            .RefreshInterval(options.RefreshInterval);

                    await Request(c => c.Indices.PutMappingAsync<T>(map, cancellationToken));
                    await Request(c => c.Indices.UpdateSettingsAsync(index.IndexName, x => x.IndexSettings(settings), cancellationToken));

                    _logger.LogInformation($"Updated index {index} in {measure}: {type.FullName}");
                }

                // index creation
                else
                {
                    ICreateIndexRequest map(CreateIndexDescriptor i)
                        => i.Settings(settings)
                            .Map(m => m.AutoMap<T>()
                                       .SourceField(s => s.Excludes(excludeFields))
                                       .Dynamic(false));

                    IPromise<IIndexSettings> settings(IndexSettingsDescriptor s)
                        => s.NumberOfShards(options.ShardCount)
                            .NumberOfReplicas(options.ReplicaCount)
                            .RefreshInterval(options.RefreshInterval);

                    await Request(c => c.Indices.CreateAsync(index.IndexName, map, cancellationToken));

                    _logger.LogInformation($"Created index {index} in {measure}: {type.FullName}");
                }

                return _indexes[type] = index;
            }
        }

        public async Task ResetAsync(CancellationToken cancellationToken = default)
        {
            foreach (var (type, index) in _indexes)
            {
                await using (await _locker.EnterAsync($"elastic:index:{index}", cancellationToken))
                {
                    if (!_indexes.TryRemove(type, out _))
                        continue;

                    var measure = new MeasureContext();

                    try
                    {
                        await Request(c => c.Indices.DeleteAsync(index.IndexName, null, cancellationToken));

                        _logger.LogInformation($"Deleted index {index} in {measure}.");
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, $"Could not delete index {index}.");
                    }
                }
            }
        }

#endregion

        public IDbEntry<T> Entry<T>(string id) where T : class, IDbObject => new EntryWrapper<T>(this, id);

        /// <summary>
        /// Serializable entry object for storing in cache.
        /// </summary>
        [MessagePackObject]
        public sealed class EntryInfo<T>
        {
            [Key(0)]
            public T Value { get; set; }

            [Key(1)]
            public long? SequenceNumber { get; set; }

            [Key(2)]
            public long? PrimaryTerm { get; set; }
        }

        /// <summary>
        /// Wrapper object for consumer convenience.
        /// </summary>
        sealed class EntryWrapper<T> : IDbEntry<T> where T : class, IDbObject
        {
            readonly ElasticClient _client;
            readonly ILogger<Nest.ElasticClient> _logger;
            readonly IRedisClient _redis;

            public string Id { get; }

            T _value;
            long? _sequenceNumber;
            long? _primaryTerm;

            public T Value
            {
                get => _value;
                set
                {
                    _value = value;

                    if (value != null)
                        value.Id = Id;
                }
            }

            public EntryWrapper(ElasticClient client, string id, EntryInfo<T> info = null)
            {
                _client = client;
                _logger = client._logger;
                _redis  = client._redis;

                Id = id;

                if (info != null)
                    Refresh(info);
            }

#region Index

            public Task<T> CreateAsync(CancellationToken cancellationToken = default)
                => IndexAsyncInternal(OpType.Create, cancellationToken);

            public Task<T> UpdateAsync(CancellationToken cancellationToken = default)
                => IndexAsyncInternal(OpType.Index, cancellationToken);

            async Task<T> IndexAsyncInternal(OpType type, CancellationToken cancellationToken = default)
            {
                if (Value == null)
                    throw new InvalidOperationException($"Cannot create or update entry when {nameof(Value)} is null.");

                var measure = new MeasureContext();
                var options = _client._options.CurrentValue;

                var index = await _client.GetIndexAsync<T>(cancellationToken);

                // set created time
                if (_value is IHasCreatedTime hasCreatedTime && hasCreatedTime.CreatedTime == default)
                    hasCreatedTime.CreatedTime = DateTime.UtcNow;

                // set updated time
                if (_value is IHasUpdatedTime hasUpdatedTime)
                    hasUpdatedTime.UpdatedTime = DateTime.UtcNow;

                // update cached properties
                _value.UpdateCache();

                // index request
                IIndexRequest<T> selector(IndexDescriptor<T> x)
                {
                    x = x.Index(index.IndexName)
                         .Id(Id)
                         .OpType(type)
                         .Refresh(options.RequestRefreshOption);

                    if (type == OpType.Index) // create can't have versioning
                        x = x.IfSequenceNumber(_sequenceNumber)
                             .IfPrimaryTerm(_primaryTerm);

                    return x;
                }

                var response = await _client.Request(c => c.IndexAsync(_value, selector, cancellationToken));

                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug($"{(type == OpType.Index ? "Indexed" : "Created")} {index} {Id} in {measure}: {SerializeValue(Value)}");

                // update info
                var info = new EntryInfo<T>
                {
                    Value          = _value,
                    SequenceNumber = response.SequenceNumber,
                    PrimaryTerm    = response.PrimaryTerm
                };

                Refresh(info);

                // update cache
                await _redis.SetObjectAsync(index.CachePrefix + Id, info, cancellationToken: cancellationToken);

                return _value;
            }

#endregion

#region Delete

            public async Task DeleteAsync(CancellationToken cancellationToken = default)
            {
                var measure = new MeasureContext();
                var options = _client._options.CurrentValue;

                var index = await _client.GetIndexAsync<T>(cancellationToken);

                // delete request
                IDeleteRequest selector(DeleteDescriptor<T> x)
                    => x.Index(index.IndexName)
                        .IfSequenceNumber(_sequenceNumber)
                        .IfPrimaryTerm(_primaryTerm)
                        .Refresh(options.RequestRefreshOption);

                var response = await _client.Request(c => c.DeleteAsync<T>(Id, selector, cancellationToken));

                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug($"Deleted {index} {Id} in {measure}: {SerializeValue(Value)}");

                // update info
                var info = new EntryInfo<T>
                {
                    Value          = _value = default,
                    SequenceNumber = response.SequenceNumber,
                    PrimaryTerm    = response.PrimaryTerm
                };

                Refresh(info);

                // update cache
                await _redis.SetObjectAsync(index.CachePrefix + Id, info, cancellationToken: cancellationToken);
            }

#endregion

#region Refresh

            public async Task<T> RefreshAsync(CancellationToken cancellationToken = default)
            {
                var index = await _client.GetIndexAsync<T>(cancellationToken);

                // update info with fresh values
                var info = await _client.GetDirectAsync<T>(Id, cancellationToken);

                Refresh(info);

                // update cache
                await _redis.SetObjectAsync(index.CachePrefix + Id, info, cancellationToken: cancellationToken);

                return _value;
            }

            void Refresh(EntryInfo<T> info)
            {
                _value          = info.Value;
                _sequenceNumber = info.SequenceNumber;
                _primaryTerm    = info.PrimaryTerm;
            }

#endregion
        }

#region Get

        /// <summary>
        /// Retrieves entry information bypassing the cache.
        /// </summary>
        async Task<EntryInfo<T>> GetDirectAsync<T>(string id, CancellationToken cancellationToken = default) where T : class, IDbObject
        {
            var index = await GetIndexAsync<T>(cancellationToken);

            var response = await Request(c => c.GetAsync<T>(id, x => x.Index(index.IndexName).Realtime(), cancellationToken));

            return new EntryInfo<T>
            {
                Value          = response.Source,
                SequenceNumber = response.SequenceNumber,
                PrimaryTerm    = response.PrimaryTerm
            };
        }

        public async Task<IDbEntry<T>> GetEntryAsync<T>(string id, CancellationToken cancellationToken = default) where T : class, IDbObject
        {
            if (string.IsNullOrEmpty(id))
                return Entry<T>(null);

            var index = await GetIndexAsync<T>(cancellationToken);

            // try get from cache
            var info = await _redis.GetObjectAsync<EntryInfo<T>>(index.CachePrefix + id, cancellationToken);

            // get fresh value from es
            if (info == null)
            {
                info = await GetDirectAsync<T>(id, cancellationToken);

                await _redis.SetObjectAsync(index.CachePrefix + id, info, cancellationToken: cancellationToken);
            }

            return new EntryWrapper<T>(this, id, info);
        }

#endregion

#region Search

        public async Task<SearchResult<IDbEntry<T>>> SearchEntriesAsync<T>(IQueryProcessor<T> processor, CancellationToken cancellationToken = default) where T : class, IDbObject
        {
            if (!processor.Valid)
                return new SearchResult<IDbEntry<T>>
                {
                    Took  = TimeSpan.Zero,
                    Total = 0,
                    Items = Array.Empty<IDbEntry<T>>()
                };

            var measure = new MeasureContext();

            var index = await GetIndexAsync<T>(cancellationToken);

            // searching bypasses cache
            var response = await Request(c => c.SearchAsync<T>(q => q.Index(index.IndexName)
                                                                     .SequenceNumberPrimaryTerm()
                                                                     .Compose(processor.Process), cancellationToken));

            var infos = response.Hits.ToArray(h => new EntryInfo<T>
            {
                Value          = h.Source,
                SequenceNumber = h.SequenceNumber,
                PrimaryTerm    = h.PrimaryTerm
            });

            // update caches
            var cacheKeys = new RedisKey[infos.Length];

            for (var i = 0; i < infos.Length; i++)
                cacheKeys[i] = index.CachePrefix + infos[i].Value.Id;

            await _redis.SetObjectManyAsync(cacheKeys, infos, cancellationToken: cancellationToken);

            return new SearchResult<IDbEntry<T>>
            {
                Took  = measure.Elapsed,
                Total = (int) response.Total,
                Items = infos.ToArray(x => new EntryWrapper<T>(this, x.Value.Id, x) as IDbEntry<T>)
            };
        }

        sealed class StreamSearchQueryProcessor<T> : IQueryProcessor<T> where T : class, IDbObject
        {
            readonly IQueryProcessor<T> _processor;
            readonly int _offset;
            readonly int _limit;

            public bool Valid => _processor.Valid;

            public StreamSearchQueryProcessor(IQueryProcessor<T> processor, int offset, int limit)
            {
                _processor = processor;
                _offset    = offset;
                _limit     = limit;
            }

            public SearchDescriptor<T> Process(SearchDescriptor<T> descriptor) =>
                _processor.Process(descriptor)
                          .Skip(_offset) // overwrite skip and take for streams
                          .Take(_limit);
        }

        public async IAsyncEnumerable<IDbEntry<T>> SearchEntriesStreamAsync<T>(IQueryProcessor<T> processor, [EnumeratorCancellation] CancellationToken cancellationToken = default) where T : class, IDbObject
        {
            for (var i = 0;;)
            {
                var options = _options.CurrentValue;

                var result = await SearchEntriesAsync(new StreamSearchQueryProcessor<T>(processor, i, options.StreamSearchChunkSize), cancellationToken);

                if (result.Items.Length == 0)
                    break;

                foreach (var item in result.Items)
                    yield return item;

                i += result.Items.Length;
            }
        }

#endregion

#region Suggest

        public async Task<TResult> SuggestAsync<T, TResult>(ISuggestProcessor<T, TResult> processor, CancellationToken cancellationToken = default) where T : class, IDbObject, IDbSupportsAutocomplete where TResult : SuggestResult
        {
            if (!processor.Valid)
                return processor.CreateResult(new ISuggestOption<T>[0]);

            var measure = new MeasureContext();

            var index = await GetIndexAsync<T>(cancellationToken);

            var response = await Request(c => c.SearchAsync<T>(q => q.Index(index.IndexName)
                                                                     .SequenceNumberPrimaryTerm()
                                                                     .Compose(processor.Process)
                                                                     .Suggest(s => s.Completion("suggest", processor.Process)), cancellationToken));

            var result = processor.CreateResult(response.Suggest["suggest"].SelectMany(s => s.Options));

            result.Took = measure.Elapsed;

            return result;
        }

#endregion

#region Count

        public async Task<int> CountAsync<T>(CancellationToken cancellationToken = default) where T : class, IDbObject
        {
            var result = await SearchEntriesAsync(new CountQueryProcessor<T>(), cancellationToken);

            return result.Total;
        }

        sealed class CountQueryProcessor<T> : IQueryProcessor<T> where T : class, IDbObject
        {
            public SearchDescriptor<T> Process(SearchDescriptor<T> descriptor) => descriptor.Take(0);
        }

#endregion

        static string SerializeValue<T>(T value) => LowLevelRequestResponseSerializer.Instance.SerializeToString(value);

        void IDisposable.Dispose() { }
    }
}