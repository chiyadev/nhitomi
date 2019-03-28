// Copyright (c) 2018-2019 fate/loli
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

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

        public StatusUpdater(
            IOptions<AppSettings> options,
            DiscordService discord)
        {
            _settings = options.Value;
            _discord = discord;
        }

        readonly Random _rand = new Random();
        string _current;

        void cycleGame()
        {
            var index = _current == null ? -1 : Array.IndexOf(_settings.Discord.Status.Games, _current);
            int next;

            do
            {
                next = _rand.Next(_settings.Discord.Status.Games.Length);
            } while (next == index);

            _current = $"{_settings.Discord.Status.Games[next]} [{_settings.Discord.Prefix}help]";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _discord.EnsureConnectedAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                // Cycle game
                cycleGame();

                // Send update
                await _discord.Socket.SetGameAsync(_current);

                // Sleep
                await Task.Delay(
                    TimeSpan.FromMinutes(_settings.Discord.Status.UpdateInterval),
                    stoppingToken
                );
            }
        }
    }
}
