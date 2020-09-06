using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nhitomi.Database;

namespace nhitomi
{
    public interface IDynamicOptions
    {
        /// <summary>
        /// Retrieves the current server configuration as a complete mapping.
        /// </summary>
        Dictionary<string, string> GetMapping();

        /// <summary>
        /// Sets a dynamic server configuration field. Pass null value to delete the field.
        /// </summary>
        Task SetAsync(string name, string value, CancellationToken cancellationToken = default);
    }

    public class DynamicOptions : IDynamicOptions
    {
        readonly IOptionsMonitor<ServerOptions> _options;
        readonly IElasticClient _elastic;
        readonly IConfiguration _config;
        readonly ILogger<DynamicOptions> _logger;

        public DynamicOptions(IOptionsMonitor<ServerOptions> options, IElasticClient elastic, IConfiguration config, ILogger<DynamicOptions> logger)
        {
            _options = options;
            _elastic = elastic;
            _config  = config;
            _logger  = logger;
        }

        public Dictionary<string, string> GetMapping()
        {
            var dict = new Dictionary<string, string>();

            foreach (var (key, value) in _config.AsEnumerable())
            {
                if (!string.IsNullOrEmpty(value))
                    dict[key] = value;
            }

            return dict;
        }

        public async Task SetAsync(string name, string value, CancellationToken cancellationToken = default)
        {
            var entry = await _elastic.GetEntryAsync<DbCompositeConfig>(DbCompositeConfig.DefaultId, cancellationToken);

            do
            {
                entry.Value        ??= new DbCompositeConfig();
                entry.Value.Config ??= new Dictionary<string, string>();

                if (value != null)
                    entry.Value.Config[name] = value;
                else
                    entry.Value.Config.Remove(name);
            }
            while (!await entry.TryUpdateAsync(cancellationToken));

            // wait passively for options to reload
            await Task.Delay(_options.CurrentValue.DynamicConfigReloadInterval, cancellationToken);

            _logger.LogInformation($"Set server config '{name}' to '{value}'.");
        }
    }
}