using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace nhitomi.Discord
{
    public class LogHandlerService : IHostedService
    {
        readonly DiscordService _discord;
        readonly ILogger _socketLogger;
        readonly ILogger _commandLogger;

        public LogHandlerService(DiscordService discord, ILoggerFactory loggerFactory)
        {
            _discord = discord;
            _socketLogger = loggerFactory.CreateLogger<DiscordSocketClient>();
            _commandLogger = loggerFactory.CreateLogger<CommandService>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _discord.Socket.Log += HandleSocketLogAsync;
            _discord.Command.Log += HandleCommandLogAsync;

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _discord.Socket.Log -= HandleSocketLogAsync;
            _discord.Command.Log -= HandleCommandLogAsync;

            return Task.CompletedTask;
        }

        Task HandleSocketLogAsync(LogMessage message) => HandleLogAsync(_socketLogger, message);
        Task HandleCommandLogAsync(LogMessage message) => HandleLogAsync(_commandLogger, message);

        static Task HandleLogAsync(ILogger logger, LogMessage message)
        {
            var level = ConvertLogSeverity(message.Severity);

            if (message.Exception == null)
                logger.Log(level, message.Message);
            else
                logger.Log(level, message.Exception, message.Exception.Message);

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