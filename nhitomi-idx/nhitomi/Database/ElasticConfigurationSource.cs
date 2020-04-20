using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace nhitomi.Database
{
    public class ElasticConfigurationSource : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder) => new ElasticConfigurationProvider();
    }

    public class ElasticConfigurationProvider : IReloadableConfigurationProvider
    {
        ConfigurationReloadToken _reloadToken = new ConfigurationReloadToken();
        IConfigurationProvider _impl;

        public ElasticConfigurationProvider()
        {
            ResetData();
        }

        void ResetData()
        {
            var provider = new MemoryConfigurationProvider(new MemoryConfigurationSource());

            provider.Load();

            _impl = provider;
        }

        public bool TryGet(string key, out string value) => _impl.TryGet(key, out value);
        public void Set(string key, string value) => _impl.Set(key, value);

        public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath) => _impl.GetChildKeys(earlierKeys, parentPath);
        public IChangeToken GetReloadToken() => _reloadToken;

        DbCompositeConfig _lastValue;

        /// <summary>
        /// Reloads this provider asynchronously, returning a task that can be awaited on.
        /// </summary>
        public async Task LoadAsync(IServiceProvider services, CancellationToken cancellationToken = default)
        {
            var client = services.GetService<IElasticClient>();

            var config = await client.GetAsync<DbCompositeConfig>(DbCompositeConfig.DefaultId, cancellationToken);

            if (config.DeepEqualTo(Interlocked.Exchange(ref _lastValue, config)))
                return;

            if (config == null)
            {
                ResetData();
            }
            else
            {
                // use json provider for simplicity
                var provider = new JsonStreamConfigurationProvider(new JsonStreamConfigurationSource
                {
                    Stream = new MemoryStream(Encoding.Default.GetBytes(JsonConvert.SerializeObject(config, new JsonSerializerSettings
                    {
                        NullValueHandling    = NullValueHandling.Ignore,
                        DefaultValueHandling = DefaultValueHandling.Include
                    })))
                });

                provider.Load();

                _impl = provider;
            }

            Interlocked.Exchange(ref _reloadToken, new ConfigurationReloadToken()).OnReload();
        }

        void IConfigurationProvider.Load() { }
    }
}