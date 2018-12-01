// Copyright (c) 2018 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace nhitomi
{
    public class StatusUpdater : IBackgroundService
    {
        readonly AppSettings.DiscordSettings.StatusSettings _settings;
        readonly DiscordService _discord;

        public StatusUpdater(
            IOptions<AppSettings> options,
            DiscordService discord
        )
        {
            _settings = options.Value.Discord.Status;
            _discord = discord;
        }

        readonly Random _rand = new Random();
        string _current;

        void cycleGame()
        {
            int index = _current == null ? -1 : System.Array.IndexOf(_settings.Games, _current);
            int next;

            do { next = _rand.Next(_settings.Games.Length); }
            while (next == index);

            _current = _settings.Games[next] + " [n!help]";
        }

        public async Task RunAsync(CancellationToken token)
        {
            do
            {
                // Cycle game
                cycleGame();

                // Send update
                await _discord.Socket.SetGameAsync(_current);

                // Sleep
                await Task.Delay(
                    TimeSpan.FromMinutes(_settings.UpdateInterval),
                    token
                );
            }
            while (!token.IsCancellationRequested);
        }
    }
}