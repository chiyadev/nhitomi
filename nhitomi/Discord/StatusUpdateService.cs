using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace nhitomi.Discord
{
    public class StatusUpdateService : BackgroundService
    {
        readonly AppSettings _settings;
        readonly DiscordService _discord;

        public StatusUpdateService(IOptions<AppSettings> options, DiscordService discord)
        {
            _settings = options.Value;
            _discord = discord;
        }

        readonly Random _rand = new Random();
        string _current;

        void CycleGame()
        {
            var index = _current == null ? -1 : Array.IndexOf(_settings.Discord.Status.Games, _current);
            int next;

            // keep choosing if we chose the same one
            do
            {
                next = _rand.Next(_settings.Discord.Status.Games.Length);
            }
            while (next == index);

            _current = $"{_settings.Discord.Status.Games[next]} [{_settings.Discord.Prefix}help]";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _discord.WaitForReadyAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                CycleGame();

                // send update
                await _discord.SetGameAsync(_current);

                // sleep
                await Task.Delay(TimeSpan.FromMinutes(_settings.Discord.Status.UpdateInterval), stoppingToken);
            }
        }
    }
}