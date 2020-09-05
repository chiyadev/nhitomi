using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;

namespace nhitomi.Database.Migrations
{
    public abstract class MigrationBase
    {
        /// <summary>
        /// Parses the ID from a migration type name.
        /// </summary>
        public static long GetMigrationIdFromType(Type type) => long.Parse(type.Name.Substring("Migration".Length));

        /// <summary>
        /// Migration unique identifier.
        /// </summary>
        public long Id { get; }

        readonly IServiceProvider _services;
        readonly IOptionsMonitor<ElasticOptions> _options;
        readonly IElasticClient _client;
        readonly ILogger<MigrationBase> _logger;

        protected MigrationBase(IServiceProvider services, ILogger<MigrationBase> logger)
        {
            Id = GetMigrationIdFromType(GetType());

            _services = services;
            _options  = services.GetService<IOptionsMonitor<ElasticOptions>>();
            _client   = services.GetService<IElasticClient>();
            _logger   = logger;
        }

        public abstract Task RunAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the source and destination indexes to migrate, given a name like "book".
        /// </summary>
        protected async Task<(string source, string destination)> GetReindexTargets(string name, CancellationToken cancellationToken = default)
        {
            var prefix = _options.CurrentValue.IndexPrefix;

            // get all indexes with migration suffix
            var indexes = await _client.RequestAsync(c => c.Cat.IndicesAsync(x => x.Index($"{prefix}{name}-*"), cancellationToken));

            // sort indexes ascending and select the last migration that isn't this migration
            var source = indexes.Records
                                .Select(r => r.Index)
                                .Where(s => MigrationManager.TryParseIndexName(s, out _, out var id) && id != Id)
                                .OrderBy(s => s)
                                .LastOrDefault();

            if (source == null)
                throw new ArgumentException($"Could not find source index for '{name}'.");

            var destination = $"{prefix}{name}-{Id}";

            // ensure destination doesn't exist
            if ((await _client.RequestAsync(c => c.Indices.ExistsAsync(destination, null, cancellationToken))).Exists)
                throw new ArgumentException($"Destination index '{destination}' already exists.");

            _logger.LogInformation($"Reindexing source index '{source}' to destination index '{destination}'.");

            return (source, destination);
        }

        /// <summary>
        /// Indexes created by this migration.
        /// </summary>
        public List<string> IndexesCreated { get; } = new List<string>();

        /// <summary>
        /// Creates a new index by the given name and the specified mapping type.
        /// </summary>
        protected async Task CreateIndexAsync<T>(string name, CancellationToken cancellationToken = default) where T : class, IDbObject
        {
            var options = _options.CurrentValue;

            // sync this code with ElasticClient.GetIndexAsync
            var excludeFields = typeof(T).GetProperties()
                                         .Where(p => p.GetCustomAttributes().OfType<DbSourceExcludeAttribute>().Any())
                                         .ToArray(p => p.GetCustomAttributes().OfType<IPropertyMapping>().First().Name);

            ICreateIndexRequest map(CreateIndexDescriptor i)
                => i.Settings(settings)
                    .Map(m => m.AutoMap<T>()
                               .SourceField(s => s.Excludes(excludeFields))
                               .Dynamic(false));

            IPromise<IIndexSettings> settings(IndexSettingsDescriptor s)
                => s.NumberOfShards(options.ShardCount)
                    .NumberOfReplicas(options.ReplicaCount)
                    .RefreshInterval(options.RefreshInterval);

            await _client.RequestAsync(c => c.Indices.CreateAsync(name, map, cancellationToken));

            IndexesCreated.Add(name);

            _logger.LogInformation($"Created index '{name}' of type {typeof(T)}.");
        }

        /// <summary>
        /// Maps an existing index of type <typeparamref name="TSource"/> to a new destination index of type <typeparamref name="TDestination"/>.
        /// </summary>
        protected async Task MapIndexAsync<TSource, TDestination>(string source, string destination, Func<TSource, TDestination> map, CancellationToken cancellationToken = default) where TSource : class, IDbObject where TDestination : class, IDbObject
        {
            await CreateIndexAsync<TDestination>(destination, cancellationToken);

            var options = _options.CurrentValue;

            // disable refresh and replicas
            await _client.RequestAsync(c => c.Indices.UpdateSettingsAsync(destination, x => x.IndexSettings(s => s.RefreshInterval(Time.MinusOne).NumberOfReplicas(0)), cancellationToken));

            // use semaphore to limit the number of indexing workers
            const int workers   = 16; //ThreadPool.GetMaxThreads(out var workers, out _);
            using var semaphore = new SemaphoreSlim(workers);

            _logger.LogDebug($"Using {workers} workers for indexing.");

            var exceptions = new List<Exception>();

            void checkExceptions()
            {
                lock (exceptions)
                {
                    Exception e;

                    switch (exceptions.Count)
                    {
                        case 0: return;

                        case 1:
                            e = exceptions[0];
                            break;

                        default:
                            e = new AggregateException(exceptions);
                            break;
                    }

                    ExceptionDispatchInfo.Throw(e);
                }
            }

            var response = null as ISearchResponse<TSource>;

            try
            {
                const string scrollDuration = "1m";

                var searchMeasure = new MeasureContext();
                var batches       = 0;
                var progress      = 0;

                response = await _client.RequestAsync(c => c.SearchAsync<TSource>(s => s.Index(source).Size(1000).Scroll(scrollDuration), cancellationToken));

                while (response.Documents.Count != 0)
                {
                    var documents = response.Documents;
                    var total     = response.Total;

                    _logger.LogDebug($"Read batch {++batches} in {searchMeasure}, {documents.Count} items.");

                    var locker       = await semaphore.EnterAsync(cancellationToken);
                    var indexMeasure = new MeasureContext();

                    var _ = Task.Run(async () =>
                    {
                        using (locker)
                        {
                            try
                            {
                                var values = documents.Select(d =>
                                {
                                    var value = map(d);

                                    value?.UpdateCache(_services);
                                    return value;
                                }).Where(d => d != null);

                                await _client.RequestAsync(c => c.IndexManyAsync(values, destination, cancellationToken));

                                lock (options)
                                {
                                    progress += documents.Count;

                                    _logger.LogInformation($"Indexed {documents.Count} items in {indexMeasure}, {progress}/{total} ({(double) progress / total:P2}).");
                                }
                            }
                            catch (Exception e)
                            {
                                lock (exceptions)
                                    exceptions.Add(e);
                            }
                        }
                    }, cancellationToken);

                    searchMeasure = new MeasureContext();
                    response      = await _client.RequestAsync(c => c.ScrollAsync<TSource>(scrollDuration, response.ScrollId, null, cancellationToken));

                    checkExceptions();
                }
            }
            finally
            {
                // clear scroll
                if (response != null)
                    await _client.RequestAsync(c => c.ClearScrollAsync(s => s.ScrollId(response.ScrollId), cancellationToken));

                // wait for all workers to exit
                for (var i = 0; i < workers; i++)
                    await semaphore.WaitAsync(cancellationToken);

                checkExceptions();

                _logger.LogInformation($"Refreshing index '{destination}'...");

                // refresh immediately
                await _client.RequestAsync(c => c.Indices.RefreshAsync(destination, null, cancellationToken));
                await _client.RequestAsync(c => c.Indices.UpdateSettingsAsync(destination, x => x.IndexSettings(s => s.RefreshInterval(options.RefreshInterval).NumberOfReplicas(options.ReplicaCount)), cancellationToken));
            }
        }
    }
}