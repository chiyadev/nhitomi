// Copyright (c) 2018-2019 fate/loli
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using Discord;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace nhitomi
{
    public class DiscordLogService : ILoggerProvider
    {
        readonly DiscordService _discord;
        readonly AppSettings _settings;

        readonly Task _worker;
        readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public DiscordLogService(
            DiscordService discord,
            IOptions<AppSettings> settings)
        {
            _discord = discord;
            _settings = settings.Value;

            _worker = runAsync(_tokenSource.Token);
        }

        readonly ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();

        async Task runAsync(CancellationToken token)
        {
            do
            {
                if (_discord.Socket.ConnectionState == ConnectionState.Connected &&
                    _discord.Socket.GetChannel(_settings.Discord.Guild.LogChannelId) is ITextChannel channel)
                {
                    var builder = new StringBuilder(500, 2000);

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
                }

                // Sleep
                await Task.Delay(TimeSpan.FromSeconds(1), token);
            } while (!token.IsCancellationRequested);
        }

        sealed class DiscordLogger : ILogger
        {
            readonly string _category;
            readonly DiscordLogService _provider;

            public DiscordLogger(
                DiscordLogService provider,
                string category)
            {
                _category = category.Split('.').Last();
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

                if (exception?.StackTrace != null)
                    text
                        .AppendLine()
                        .Append("Trace: ")
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
