using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChiyaFlake;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nhitomi.Controllers;
using nhitomi.Database;
using nhitomi.Database.Migrations;
using nhitomi.Models;
using nhitomi.Models.Queries;
using nhitomi.Storage;

namespace nhitomi
{
    public class StartupInitializer
    {
        readonly IOptionsMonitor<UserServiceOptions> _options;
        readonly IServiceProvider _services;
        readonly IRedisClient _redis;
        readonly IElasticClient _elastic;
        readonly IStorage _storage;
        readonly IAuthService _auth;
        readonly IMigrationManager _migrations;
        readonly IReloadableConfigurationProvider[] _configProviders;
        readonly ILogger<StartupInitializer> _logger;

        public StartupInitializer(IOptionsMonitor<UserServiceOptions> options, IServiceProvider services, IRedisClient redis, IElasticClient elastic, IStorage storage, IAuthService auth, IMigrationManager migrations, IConfigurationRoot config, ILogger<StartupInitializer> logger)
        {
            _options         = options;
            _services        = services;
            _redis           = redis;
            _elastic         = elastic;
            _storage         = storage;
            _auth            = auth;
            _migrations      = migrations;
            _configProviders = config.Providers.OfType<IReloadableConfigurationProvider>().ToArray();
            _logger          = logger;
        }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            await InitRedisAsync(cancellationToken);
            await InitElasticAsync(cancellationToken);

            await _migrations.FinalizeAsync(cancellationToken);

            // load dynamic configurations
            await Task.WhenAll(_configProviders.Select(p => p.LoadAsync(_services, cancellationToken)));

            await InitStorageAsync(cancellationToken);
            await ConfigureUsersAsync(cancellationToken);
            await ConfigureAuthAsync(cancellationToken);
        }

        public bool SanityChecks { get; set; } = true;

        async Task InitRedisAsync(CancellationToken cancellationToken = default)
        {
            await _redis.InitializeAsync(cancellationToken);

            if (!SanityChecks)
                return;

            var key  = $"sanity_check_{Snowflake.New}";
            var data = new byte[] { 1, 2, 3, 4 };

            await _redis.SetAsync(key, data, TimeSpan.FromMinutes(1), cancellationToken: cancellationToken);

            try
            {
                var result = await _redis.GetAsync(key, cancellationToken);

                if (!result.BufferEquals(data))
                    throw new ApplicationException("Redis sanity check failed.");

                _logger.LogWarning("Redis sanity check succeeded.");
            }
            finally
            {
                await _redis.DeleteAsync(key, cancellationToken);
            }
        }

        async Task InitElasticAsync(CancellationToken cancellationToken = default)
        {
            await _elastic.InitializeAsync(cancellationToken);

            if (!SanityChecks)
                return;

            var count = await _elastic.CountAsync<DbUser>(cancellationToken);

            if (count < 0)
                throw new ApplicationException("Elasticsearch sanity check failed.");

            _logger.LogWarning("Elasticsearch sanity check succeeded.");
        }

        async Task InitStorageAsync(CancellationToken cancellationToken = default)
        {
            await _storage.InitializeAsync(cancellationToken);

            if (!SanityChecks)
                return;

            var key  = $"sanity_check_{Snowflake.New}";
            var data = key;

            await _storage.WriteStringAsync(key, data, cancellationToken);

            try
            {
                var result = await _storage.ReadStringAsync(key, cancellationToken);

                if (result != data)
                    throw new ApplicationException("Storage sanity check failed.");

                _logger.LogWarning("Storage sanity check succeeded.");
            }
            finally
            {
                await _storage.DeleteAsync(key, cancellationToken);
            }
        }

        public async Task ConfigureUsersAsync(CancellationToken cancellationToken = default)
        {
            if (!_options.CurrentValue.FirstUserAdmin)
                return;

            // assign admin to the first user
            var result = await _elastic.SearchEntriesAsync(new DbUserQueryProcessor(new UserQuery
            {
                Limit = 1,
                Sorting = new List<SortField<UserSort>>
                {
                    (UserSort.CreatedTime, SortDirection.Ascending)
                }
            }), cancellationToken);

            if (result.Items.Length != 0)
            {
                var entry = result.Items[0];

                do
                {
                    if (entry.Value.HasPermissions(UserPermissions.Administrator))
                        break;

                    entry.Value.Permissions = new[] { UserPermissions.Administrator };
                }
                while (!await entry.TryUpdateAsync(cancellationToken));
            }
        }

        public async Task ConfigureAuthAsync(CancellationToken cancellationToken = default)
        {
            var str = await _storage.ReadStringAsync("meta/secret", cancellationToken);

            if (str == null)
            {
                // for older versions of nhitomi: migrate signing key stored in redis
                var redisSignKey = await _redis.GetAsync("config:signKey", cancellationToken);

                if (redisSignKey != null)
                {
                    str = Convert.ToBase64String(_auth.SigningSecret = redisSignKey);

                    await _redis.DeleteAsync("config:signKey", cancellationToken);
                }
                else
                {
                    str = Convert.ToBase64String(_auth.SigningSecret);
                }

                await _storage.WriteStringAsync("meta/secret", str, cancellationToken);
            }
            else
            {
                _auth.SigningSecret = Convert.FromBase64String(str);
            }
        }
    }
}