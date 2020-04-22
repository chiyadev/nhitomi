using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using nhitomi.Controllers;
using nhitomi.Database;
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
        readonly IReloadableConfigurationProvider[] _configProviders;

        public StartupInitializer(IOptionsMonitor<UserServiceOptions> options, IServiceProvider services, IRedisClient redis, IElasticClient elastic, IStorage storage, IConfigurationRoot config)
        {
            _options         = options;
            _services        = services;
            _redis           = redis;
            _elastic         = elastic;
            _storage         = storage;
            _configProviders = config.Providers.OfType<IReloadableConfigurationProvider>().ToArray();
        }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            // initialize redis and elasticsearch
            await _redis.InitializeAsync(cancellationToken);
            await _elastic.InitializeAsync(cancellationToken);

            // load dynamic configurations
            await Task.WhenAll(_configProviders.Select(p => p.LoadAsync(_services, cancellationToken)));

            // initialize storage
            await _storage.InitializeAsync(cancellationToken);

            await ConfigureUsersAsync(cancellationToken);
        }

        public async Task ConfigureUsersAsync(CancellationToken cancellationToken = default)
        {
            // make admin user
            if (_options.CurrentValue.FirstUserAdmin)
            {
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
        }
    }
}