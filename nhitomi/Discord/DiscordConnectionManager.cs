using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace nhitomi.Discord
{
    /// <summary>
    /// Service responsible for connecting the Discord client.
    /// </summary>
    public class DiscordConnectionManager : BackgroundService
    {
        readonly IDiscordClient _client;
        readonly IOptionsMonitor<DiscordOptions> _options;

        public DiscordConnectionManager(IDiscordClient client, IOptionsMonitor<DiscordOptions> options)
        {
            _client  = client;
            _options = options;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var options = _options.CurrentValue;

            if (!options.Enabled)
                return;

            await _client.LoginAsync(TokenType.Bot, options.Token);
            await _client.StartAsync();

            try
            {
                await Task.Delay(-1, stoppingToken);
            }
            finally
            {
                await _client.StopAsync();
                await _client.LogoutAsync();
            }
        }
    }
}