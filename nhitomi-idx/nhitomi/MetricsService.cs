using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prometheus;
using Prometheus.DotNetRuntime;

namespace nhitomi
{
    public interface IMetricsService : IHostedService { }

    public class MetricsService : BackgroundService, IMetricsService
    {
        readonly ILogger<MetricsService> _logger;

        readonly IDisposable _runtimeStatsRegistration;
        readonly int _port;
        readonly KestrelMetricServer _server;

        public MetricsService(IOptionsMonitor<ServerOptions> options, ILogger<MetricsService> logger)
        {
            _logger = logger;

            _runtimeStatsRegistration = DotNetRuntimeStatsBuilder.Default().StartCollecting();

            var port = options.CurrentValue.MetricsPort;

            if (port != null)
                _server = new KestrelMetricServer(_port = port.Value);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_server == null)
                return;

            _logger.LogInformation($"Publishing Prometheus metrics on port {_port}.");

            _server.Start();

            try
            {
                await Task.Delay(-1, stoppingToken);
            }
            finally
            {
                await _server.StopAsync();
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            _runtimeStatsRegistration.Dispose();
        }
    }
}