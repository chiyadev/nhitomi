using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

        readonly IOptionsMonitor<ElasticOptions> _options;
        readonly Nest.ElasticClient _client;
        readonly ILogger<MigrationBase> _logger;

        protected MigrationBase(IOptionsMonitor<ElasticOptions> options, IElasticClient client, ILogger<MigrationBase> logger)
        {
            Id = GetMigrationIdFromType(GetType());

            _options = options;
            _client  = client.GetInternalClient();
            _logger  = logger;
        }

        public abstract Task RunAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the source and destination indexes to migrate, given a name like "book".
        /// </summary>
        protected async Task<(string source, string destination)> GetReindexTargets(string name, CancellationToken cancellationToken = default)
        {
            var prefix = _options.CurrentValue.IndexPrefix;

            // get all indexes with migration suffix
            var indexes = await _client.Cat.IndicesAsync(c => c.Index($"{prefix}{name}*"), cancellationToken);

            // sort indexes ascending and select the last migration that isn't this migration
            var source = indexes.Records
                                .Select(r => r.Index)
                                .Where(s => MigrationManager.TryParseIndexName(s, out _, out var id) && id != Id)
                                .OrderBy(s => s)
                                .LastOrDefault();

            if (source == null)
                throw new ArgumentException($"Could not find source index for '{name}'.");

            var destination = $"{prefix}{name}{Id}";

            // ensure destination doesn't exist
            if ((await _client.Indices.ExistsAsync(destination, null, cancellationToken)).Exists)
                throw new ArgumentException($"Destination index '{destination}' already exists.");

            _logger.LogInformation($"Reindex destination '{destination}' selected for source '{source}'.");

            return (source, destination);
        }

        /// <summary>
        /// Indexes created by this migration.
        /// </summary>
        public List<string> IndexesCreated { get; } = new List<string>();
    }
}