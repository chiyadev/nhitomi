using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IO;
using Nest;

namespace nhitomi.Database.Migrations
{
    public interface IMigrationManager
    {
        /// <summary>
        /// Migrates the database, writing new data in separated migration indexes.
        /// To achieve zero downtime migration, indexes are not deleted until finalize is called, allowing an existing older instance of nhitomi to keep running.
        /// </summary>
        Task RunAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes old migration indexes and leaves the latest supported migration.
        /// </summary>
        Task FinalizeAsync(CancellationToken cancellationToken = default);
    }

    public class MigrationManager : IMigrationManager
    {
        /// <summary>
        /// All migrations in the current version of nhitomi.
        /// </summary>
        public static readonly IReadOnlyDictionary<long, Type> MigrationTypes =
            typeof(Startup).Assembly
                           .GetTypes()
                           .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(MigrationBase)))
                           .ToDictionary(MigrationBase.GetMigrationIdFromType);

        /// <summary>
        /// Latest migration ID in the current version of nhitomi.
        /// </summary>
        public static readonly long LatestMigrationId = MigrationTypes.Keys.OrderByDescending(x => x).First();

        readonly IServiceProvider _services;
        readonly IResourceLocker _locker;
        readonly IOptionsMonitor<ElasticOptions> _options;
        readonly IWriteControl _writeControl;
        readonly Nest.ElasticClient _client;
        readonly ILogger<MigrationManager> _logger;

        public MigrationManager(IServiceProvider services, IResourceLocker locker, IOptionsMonitor<ElasticOptions> options, IWriteControl writeControl, RecyclableMemoryStreamManager memory, ILogger<MigrationManager> logger)
        {
            _services     = services;
            _locker       = locker;
            _options      = options;
            _writeControl = writeControl;
            _logger       = logger;

            var pool = new SingleNodeConnectionPool(new Uri($"http://{options.CurrentValue.Endpoint}"));

            _client = new Nest.ElasticClient(
                new ConnectionSettings(pool)
                   .ThrowExceptions(false)
                   .MemoryStreamFactory(new ElasticMemoryStreamFactory(memory)));
        }

        /// <summary>
        /// Attempts to parse index names in the format "{name}-{migrationId}".
        /// </summary>
        public static bool TryParseIndexName(string index, out string name, out long migrationId)
        {
            name        = index;
            migrationId = 0;

            var dash = index.LastIndexOf('-');

            if (dash == -1)
                return false;

            name = index.Substring(0, dash);

            return long.TryParse(index.Substring(dash + 1), out migrationId);
        }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            // block database writes
            await _writeControl.BlockAsync(cancellationToken);

            await using (await _locker.EnterAsync("maintenance:migrations", cancellationToken))
            {
                var options = _options.CurrentValue;
                var indexes = await _client.Cat.IndicesAsync(c => c.Index($"{options.IndexPrefix}*"), cancellationToken);
                var count   = 0;

                // not all indexes get migrated every migration, so use the max
                var lastMigrationId = indexes.Records.Select(r => TryParseIndexName(r.Index, out _, out var migrationId) ? migrationId : 0).Max();

                foreach (var nextMigrationId in MigrationTypes.Keys.OrderBy(x => x))
                {
                    if (nextMigrationId <= lastMigrationId)
                        continue;

                    var migration = (MigrationBase) ActivatorUtilities.CreateInstance(_services, MigrationTypes[nextMigrationId]);

                    _logger.LogWarning($"Applying migration {migration.Id}.");

                    try
                    {
                        await migration.RunAsync(cancellationToken);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"Could not apply migration {migration.Id}.");

                        foreach (var index in migration.IndexesCreated)
                        {
                            try
                            {
                                await _client.Indices.DeleteAsync(index, null, cancellationToken);

                                _logger.LogInformation($"Deleted incomplete index '{index}'.");
                            }
                            catch (Exception ee)
                            {
                                _logger.LogWarning(ee, $"Could not delete incomplete index '{index}'.");
                            }
                        }

                        break;
                    }

                    count++;
                }

                _logger.LogInformation($"All {count} migration(s) applied.");
            }
        }

        public async Task FinalizeAsync(CancellationToken cancellationToken = default)
        {
            //todo: clear redis caches
            //todo: disable scrapers while migrating

            var options = _options.CurrentValue;
            var indexes = await _client.Cat.IndicesAsync(c => c.Index($"{options.IndexPrefix}*"), cancellationToken);

            var migrationIds = new Dictionary<string, long>(indexes.Records.Count);

            foreach (var index in indexes.Records)
            {
                if (!TryParseIndexName(index.Index, out var name, out var currentId))
                    continue;

                // unknown migration
                if (!MigrationTypes.ContainsKey(currentId))
                    continue;

                // delete older migration index
                if (migrationIds.TryGetValue(name, out var lastId) && currentId < lastId)
                {
                    var response = await _client.Indices.DeleteAsync(index.Index, null, cancellationToken);

                    if (response.OriginalException != null)
                        _logger.LogWarning(response.OriginalException, $"Could not delete old migration index '{name}-{lastId}'.");
                }
                else
                {
                    migrationIds[name] = currentId;
                }
            }

            // allow database writes
            await _writeControl.UnblockAsync(cancellationToken);
        }
    }
}