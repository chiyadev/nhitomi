// Copyright (c) 2018-2019 fate/loli
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nhitomi.Core;

namespace nhitomi.Services
{
    public class FeedUpdater : BackgroundService
    {
        readonly AppSettings _settings;
        readonly ISet<IDoujinClient> _clients;
        readonly DiscordService _discord;
        readonly MessageFormatter _formatter;
        readonly ILogger<FeedUpdater> _logger;

        public FeedUpdater(
            IOptions<AppSettings> options,
            ISet<IDoujinClient> clients,
            DiscordService discord,
            MessageFormatter formatter,
            ILogger<FeedUpdater> logger)
        {
            _settings = options.Value;
            _clients = clients;
            _discord = discord;
            _formatter = formatter;
            _logger = logger;
        }

        readonly ConcurrentDictionary<IDoujinClient, IDoujin> _lastDoujins =
            new ConcurrentDictionary<IDoujinClient, IDoujin>();

        async Task<IAsyncEnumerable<IDoujin>> FindNewDoujinsAsync(CancellationToken cancellationToken = default)
        {
            var doujins = Extensions.Interleave(await Task.WhenAll(_clients.Select(async c =>
            {
                IDoujin current = null;

                try
                {
                    if (!_lastDoujins.TryGetValue(c, out var last))
                        _lastDoujins[c] = null;

                    // Get all new doujins up to the last one we know
                    var list =
                        (await c.SearchAsync(null, cancellationToken))
                        .TakeWhile(d => d?.Id != last?.Id);

                    current = await list.FirstOrDefault(cancellationToken) ?? last;

                    if (current != last && last != null)
                        return list;
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, $"Exception while searching client '{c.Name}': {e.Message}");

                    current = null;
                }
                finally
                {
                    _lastDoujins[c] = current;

                    _logger.LogDebug($"Most recent doujin: [{c.Name}] {current?.PrettyName ?? "<null>"}");
                }

                return AsyncEnumerable.Empty<IDoujin>();
            })));

            if (_settings.Doujin.MaxFeedUpdatesCount > 0)
                doujins = doujins.Take(_settings.Doujin.MaxFeedUpdatesCount);

            return doujins;
        }

        async Task SendUpdateAsync(IMessageChannel channel, IDoujin doujin)
        {
            try
            {
                var message = await channel.SendMessageAsync(embed: _formatter.CreateDoujinEmbed(doujin));

                await _formatter.AddFeedDoujinTriggersAsync(message);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e,
                    $"Exception while sending feed message for doujin '{doujin.OriginalName ?? doujin.PrettyName}'");
            }
        }

        async Task SendTagUpdatesAsync(
            IEnumerable<IDoujin> doujins,
            CancellationToken cancellationToken = default)
        {
            // get feed channels
            var channels =
                (_discord.Socket.GetChannel(_settings.Discord.Guild.FeedCategoryId) as SocketCategoryChannel)
                ?.Channels.OfType<ITextChannel>().ToArray();

            if (channels == null || channels.Length == 0)
                return;

            _logger.LogDebug($"Found tag feed channels: {string.Join(", ", channels.Select(c => c.Name))}");

            foreach (var doujin in doujins)
            {
                if (doujin.Tags == null)
                    return;

                var tags = GetTagChannelNames(doujin).ToArray();

                await Task.WhenAll(channels
                    .Where(c => System.Array.IndexOf(tags, c.Name) != -1)
                    .Select(c => SendUpdateAsync(c, doujin)));

                _logger.LogDebug($"Send doujin update by tag '{doujin.OriginalName ?? doujin.PrettyName}'");
            }
        }

        async Task SendLanguageUpdatesAsync(
            IEnumerable<IDoujin> doujins,
            CancellationToken cancellationToken = default)
        {
            // get feed channels
            var channels =
                (_discord.Socket.GetChannel(_settings.Discord.Guild.LanguageFeedCategoryId) as SocketCategoryChannel)
                ?.Channels.OfType<ITextChannel>().ToArray();

            if (channels == null || channels.Length == 0)
                return;

            _logger.LogDebug($"Found language feed channels: {string.Join(", ", channels.Select(c => c.Name))}");

            foreach(var doujin in doujins)
            {
                if (doujin.Language == null)
                    return;

                var language = GetLanguageChannelName(doujin);
                var index = System.Array.FindIndex(channels, c => c.Name == language);

                if (index != -1)
                    await SendUpdateAsync(channels[index], doujin);

                _logger.LogDebug($"Send doujin update by language '{doujin.OriginalName ?? doujin.PrettyName}'");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _discord.EnsureConnectedAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                // find new doujins
                var doujins = await (await FindNewDoujinsAsync(stoppingToken)).ToArray(stoppingToken);

                // send
                await SendTagUpdatesAsync(doujins, stoppingToken);
                await SendLanguageUpdatesAsync(doujins, stoppingToken);

                // Sleep
                await Task.Delay(
                    TimeSpan.FromMinutes(_settings.Doujin.FeedUpdateInterval),
                    stoppingToken);
            }
        }

        static IEnumerable<string> GetTagChannelNames(IDoujin doujin) =>
            doujin.Tags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t
                    .ToLowerInvariant()
                    .Replace(' ', '-'));

        static string GetLanguageChannelName(IDoujin doujin) =>
            doujin.Language
                .ToLowerInvariant()
                .Replace(' ', '-');
    }
}
