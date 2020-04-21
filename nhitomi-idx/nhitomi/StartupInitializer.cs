using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using nhitomi.Database;
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

            if (_options.CurrentValue.InitializeFirstUser)
                await ActivatorUtilities.CreateInstance<UserInitializer>(_services).RunAsync(cancellationToken);
        }
    }
}