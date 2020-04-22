using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace nhitomi.Database
{
    public class ElasticConfigurationSource : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder) => new ElasticConfigurationProvider();
    }

    public class ElasticConfigurationProvider : ConfigurationProvider, IReloadableConfigurationProvider
    {
        /// <summary>
        /// Reloads this provider asynchronously, returning a task that can be awaited on.
        /// </summary>
        public async Task LoadAsync(IServiceProvider services, CancellationToken cancellationToken = default)
        {
            var client  = services.GetService<IElasticClient>();
            var options = services.GetService<IOptionsMonitor<ElasticOptions>>().CurrentValue;

            if (!options.EnableDynamicConfig)
            {
                if (Data.Count != 0)
                {
                    Data = new Dictionary<string, string>();
                    OnReload();
                }

                return;
            }

            var config = await client.GetAsync<DbCompositeConfig>(DbCompositeConfig.DefaultId, cancellationToken);

            Data = config?.Config ?? new Dictionary<string, string>();
            OnReload();
        }
    }
}