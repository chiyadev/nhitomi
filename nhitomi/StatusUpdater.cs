using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace nhitomi
{
    public class StatusUpdater : BackgroundService
    {
        readonly AppSettings _settings;
        readonly DiscordService _discord;

        public StatusUpdater(IOptions<AppSettings> options, DiscordService discord)
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
            //todo: only update status if we are shard 0
            await _discord.ConnectAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                CycleGame();

                // send update
                await _discord.Socket.SetGameAsync(_current);

                // sleep
                await Task.Delay(TimeSpan.FromMinutes(_settings.Discord.Status.UpdateInterval), stoppingToken);
            }
        }
    }
}