// Copyright (c) 2018-2019 chiya.dev
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace nhitomi
{
    public class DiscordService : IDisposable
    {
        readonly IServiceProvider _services;
        readonly AppSettings _settings;
        readonly MessageFormatter _formatter;
        readonly ILogger<DiscordService> _logger;

        readonly Regex _galleryRegex;

        public DiscordSocketClient Socket { get; }
        public CommandService Commands { get; }

        public DiscordService(IServiceProvider services, IOptions<AppSettings> options, MessageFormatter formatter,
            ILoggerFactory loggerFactory, IHostingEnvironment environment)
        {
            _services = services;
            _settings = options.Value;
            _formatter = formatter;

            // build gallery regex to match all known formats
            _galleryRegex = new Regex(GalleryRegex.Combined, RegexOptions.Compiled);

            //todo: sharding
            Socket = new DiscordSocketClient(_settings.Discord);
            Commands = new CommandService(_settings.Discord.Command);

            // log uploading in production
            if (environment.IsProduction())
                loggerFactory.AddProvider(new DiscordLogService(this, options));

            _logger = loggerFactory.CreateLogger<DiscordService>();
            _logger.LogDebug($"Gallery match regex: {_galleryRegex}");
        }

        readonly SemaphoreSlim _connectionSemaphore = new SemaphoreSlim(1);

        public async Task ConnectAsync()
        {
            await _connectionSemaphore.WaitAsync();
            try
            {
                if (Socket.ConnectionState != ConnectionState.Disconnected)
                    return;

                // events
                Socket.MessageReceived += HandleMessageAsync;
                Socket.Log += HandleLogAsync;
                Commands.Log += HandleLogAsync;

                // command modules
                await Commands.AddModulesAsync(typeof(Program).Assembly, _services);

                _logger.LogDebug($"Loaded commands: {string.Join(", ", Commands.Commands.Select(c => c.Name))}");

                var connectionSource = new TaskCompletionSource<object>();

                Socket.Ready += handleReady;

                Task handleReady()
                {
                    connectionSource.SetResult(null);
                    return Task.CompletedTask;
                }

                // login
                await Socket.LoginAsync(TokenType.Bot, _settings.Discord.Token);
                await Socket.StartAsync();

                // wait until ready signal
                await connectionSource.Task;

                Socket.Connected -= handleReady;
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        public async Task DisconnectAsync()
        {
            await _connectionSemaphore.WaitAsync();
            try
            {
                // events
                Socket.MessageReceived += HandleMessageAsync;
                Socket.Log -= HandleLogAsync;
                Commands.Log -= HandleLogAsync;

                // logout
                await Socket.StopAsync();
                await Socket.LogoutAsync();
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        Task HandleMessageAsync(SocketMessage message)
        {
            // ignore bot messages or self messages
            if (!(message is SocketUserMessage userMessage) ||
                message.Author.Id == Socket.CurrentUser.Id)
                return Task.CompletedTask;

            Task handler;

            // received message with command prefix
            var argIndex = 0;

            if (userMessage.HasStringPrefix(_settings.Discord.Prefix, ref argIndex) ||
                userMessage.HasMentionPrefix(Socket.CurrentUser, ref argIndex))
                handler = ExecuteCommandAsync(userMessage, argIndex);

            // received an arbitrary message
            // scan for gallery urls and display doujin info
            else handler = DetectGalleryUrlsAsync(userMessage);

            // run handler in background to not block the gateway thread
            _ = Task.Run(() => handler);

            return Task.CompletedTask;
        }

        async Task ExecuteCommandAsync(SocketUserMessage message, int argIndex)
        {
            // execute command
            var context = new SocketCommandContext(Socket, message);
            var result = await Commands.ExecuteAsync(context, argIndex, _services);

            // check for any errors during command execution
            if (result.Error == CommandError.Exception)
            {
                var exception = ((ExecuteResult) result).Exception;

                _logger.LogWarning(exception, "Exception while handling message {0}.", message.Id);

                // notify the user about this error
                await message.Channel.SendMessageAsync(embed: _formatter.CreateErrorEmbed());
            }
        }

        public delegate Task GalleryDetectionHandler(IUserMessage message, (string source, string id)[] ids);

        public event GalleryDetectionHandler DoujinsDetected;

        async Task DetectGalleryUrlsAsync(IUserMessage message)
        {
            var content = message.Content;

            // try recognizing at least one gallery url
            if (!_galleryRegex.IsMatch(content))
                return;

            var ids = _galleryRegex
                .Matches(content)
                .SelectMany(m => m.Groups)
                .Where(g => g.Name != null && g.Name.StartsWith("source_"))
                .Select(g => (g.Name.Split('_', 2)[1], g.Value))
                .ToArray();

            if (DoujinsDetected != null)
                await DoujinsDetected(message, ids);
        }

        Task HandleLogAsync(LogMessage m)
        {
            var level = ConvertLogSeverity(m.Severity);

            if (m.Exception == null)
                _logger.Log(level, m.Message);
            else
                _logger.Log(level, m.Exception, m.Exception.Message);

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

        public void Dispose() => Socket.Dispose();
        // Commands.Dispose does not exist
    }
}