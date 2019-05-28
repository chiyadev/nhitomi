using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace nhitomi.Discord
{
    public class LogHandlerService : IHostedService
    {
        readonly DiscordService _discord;
        readonly ILogger _logger;

        public LogHandlerService(DiscordService discord, ILoggerFactory loggerFactory)
        {
            _discord = discord;
            _logger = loggerFactory.CreateLogger<DiscordSocketClient>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _discord.Log += HandleLogAsync;

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _discord.Log -= HandleLogAsync;

            return Task.CompletedTask;
        }

        Task HandleLogAsync(LogMessage message)
        {
            var level = ConvertLogSeverity(message.Severity);

            if (message.Exception == null)
                _logger.Log(level, message.Message);
            else
                _logger.Log(level, message.Exception, message.Exception.Message);

            return Task.CompletedTask;
        }

        static LogLevel ConvertLogSeverity(LogSeverity severity)
        {
            switch (severity)
            {
                case LogSeverity.Verbose: return LogLevel.Trace;
                case LogSeverity.Debug: return LogLevel.Debug;
                case LogSeverity.Info: return LogLevel.Information;
                case LogSeverity.Warning: return LogLevel.Warning;
                case LogSeverity.Error: return LogLevel.Error;
                case LogSeverity.Critical: return LogLevel.Critical;

                default:
                    return LogLevel.None;
            }
        }
    }
}