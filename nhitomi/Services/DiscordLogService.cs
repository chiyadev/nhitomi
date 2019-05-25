// Copyright (c) 2018-2019 chiya.dev
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace nhitomi.Services
{
    public class DiscordLogService : ILoggerProvider
    {
        readonly DiscordService _discord;
        readonly AppSettings _settings;

        readonly Task _worker;
        readonly CancellationTokenSource _workerTokenSource = new CancellationTokenSource();

        public DiscordLogService(
            DiscordService discord,
            IOptions<AppSettings> settings)
        {
            _discord = discord;
            _settings = settings.Value;

            _worker = RunAsync(_workerTokenSource.Token);
        }

        readonly ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();
        readonly ConcurrentQueue<string> _warningQueue = new ConcurrentQueue<string>();

        async Task UploadLogs(
            ulong channelId,
            ConcurrentQueue<string> queue,
            CancellationToken cancellationToken = default)
        {
            if (_discord.Socket.ConnectionState == ConnectionState.Connected &&
                _discord.Socket.GetChannel(channelId) is ITextChannel channel)
            {
                var builder = new StringBuilder(500, 2000);

                async Task flush()
                {
                    if (builder.Length == 0 ||
                        cancellationToken.IsCancellationRequested)
                        return;

                    await channel.SendMessageAsync(builder.ToString());
                    builder.Clear();
                }

                // upload logs in chunks to fit 2000 character limit
                while (queue.TryDequeue(out var line))
                {
                    if (builder.Length + line.Length > 2000)
                        await flush();

                    builder.AppendLine(line);
                }

                await flush();
            }
        }

        async Task RunAsync(CancellationToken cancellationToken = default)
        {
            do
            {
                await UploadLogs(_settings.Discord.Guild.LogChannelId, _queue, cancellationToken);
                await UploadLogs(_settings.Discord.Guild.LogWarningChannelId, _warningQueue, cancellationToken);

                // Sleep
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
            while (!cancellationToken.IsCancellationRequested);
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

            static readonly Dictionary<LogLevel, string> _levelNames = new Dictionary<LogLevel, string>
            {
                {LogLevel.Debug, "dbug"},
                {LogLevel.Error, "err"},
                {LogLevel.Trace, "trce"},
                {LogLevel.Warning, "warn"},
                {LogLevel.Critical, "crit"},
                {LogLevel.Information, "info"}
            };

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception exception,
                Func<TState, Exception, string> formatter)
            {
                if (!IsEnabled(logLevel))
                    return;

                var text = new StringBuilder()
                    .Append("__")
                    .Append(_levelNames[logLevel])
                    .Append("__ **")
                    .Append(_category)
                    .Append("**: ")
                    .Append(formatter(state, exception));

                if (exception?.StackTrace != null)
                    text.AppendLine()
                        .Append(exception.Message)
                        .Append(": ")
                        .Append(exception.StackTrace);

                if (logLevel < LogLevel.Warning)
                    _provider._queue.Enqueue(text.ToString());
                else
                    _provider._warningQueue.Enqueue(text.ToString());
            }
        }

        public ILogger CreateLogger(string categoryName) => new DiscordLogger(this, categoryName);

        public void Dispose()
        {
            // Stop worker task
            _workerTokenSource.Cancel();
            _worker.Wait();

            _workerTokenSource.Dispose();
            _worker.Dispose();
        }
    }
}