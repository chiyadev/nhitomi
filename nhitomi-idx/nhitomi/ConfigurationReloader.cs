using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace nhitomi
{
    public interface IReloadableConfigurationProvider : IConfigurationProvider
    {
        Task LoadAsync(IServiceProvider services, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Periodically reloads configuration providers that implement <see cref="IReloadableConfigurationProvider"/>.
    /// </summary>
    public class ConfigurationReloader : BackgroundService
    {
        readonly IServiceProvider _services;
        readonly IReloadableConfigurationProvider[] _providers;
        readonly IOptionsMonitor<ServerOptions> _options;

        public ConfigurationReloader(IServiceProvider services, IConfigurationRoot config, IOptionsMonitor<ServerOptions> options)
        {
            _services  = services;
            _providers = config.Providers.OfType<IReloadableConfigurationProvider>().ToArray();
            _options   = options;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(_options.CurrentValue.DynamicConfigReloadInterval, stoppingToken);

                await Task.WhenAll(_providers.Select(p => p.LoadAsync(_services, stoppingToken)));
            }
        }
    }
}