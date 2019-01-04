// Copyright (c) 2018 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using Discord;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace nhitomi
{
    public class DiscordLogRedirector : ILoggerProvider
    {
        readonly AppSettings.DiscordSettings.ServerSettings _settings;
        readonly DiscordService _discord;

        readonly Task _worker;
        readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public DiscordLogRedirector(
            IOptions<AppSettings> options,
            DiscordService discord
        )
        {
            _settings = options.Value.Discord.Server;
            _discord = discord;

            _worker = runAsync(_tokenSource.Token);
        }

        readonly ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();

        async Task runAsync(CancellationToken token)
        {
            do
            {
                if (_discord.Socket.ConnectionState != ConnectionState.Connected)
                    goto sleep;

                var channel = _discord.Socket
                    .GetGuild(_settings.ServerId)
                    .GetTextChannel(_settings.LogChannelId);

                if (channel == null)
                    goto sleep;

                var builder = new StringBuilder(
                    capacity: 500,
                    maxCapacity: 2000
                );

                async Task flush()
                {
                    if (builder.Length == 0 ||
                        token.IsCancellationRequested)
                        return;

                    await channel.SendMessageAsync(builder.ToString());
                    builder.Clear();
                }

                // Chunk logs to fit 2000 characters limit
                while (_queue.TryDequeue(out var line))
                {
                    if (builder.Length + line.Length > 2000)
                        await flush();

                    builder.AppendLine(line);
                }
                await flush();

            sleep:
                // Sleep
                await Task.Delay(
                    TimeSpan.FromSeconds(1),
                    token
                );
            }
            while (!token.IsCancellationRequested);
        }

        sealed class DiscordLogger : ILogger
        {
            readonly string _category;
            readonly DiscordLogRedirector _provider;

            public DiscordLogger(
                DiscordLogRedirector provider,
                string category
            )
            {
                _category = category;
                _provider = provider;
            }

            public IDisposable BeginScope<TState>(TState state) => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception exception,
                Func<TState, Exception, string> formatter
            )
            {
                if (!IsEnabled(logLevel))
                    return;

                var text = new StringBuilder()
                    .Append(DateTimeOffset.Now.ToString("HH:mm:ss zzz"))
                    .Append($" __{logLevel}__ ")
                    .Append($" **{_category}**: ")
                    .Append(formatter(state, exception));

                if (exception.StackTrace != null)
                    text
                        .AppendLine()
                        .Append($"Trace: ")
                        .Append(exception.StackTrace);

                _provider._queue.Enqueue(text.ToString());
            }
        }

        public ILogger CreateLogger(string categoryName) => new DiscordLogger(this, categoryName);

        public void Dispose()
        {
            // Stop worker task
            _tokenSource.Cancel();
            _worker.Wait();

            _tokenSource.Dispose();
            _worker.Dispose();
        }
    }
}