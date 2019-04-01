// Copyright (c) 2018-2019 fate/loli
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
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
using nhitomi.Core;

namespace nhitomi.Services
{
    public class DiscordService : IDisposable
    {
        readonly IServiceProvider _services;
        readonly AppSettings _settings;
        readonly ISet<IDoujinClient> _clients;
        readonly MessageFormatter _formatter;
        readonly ILogger<DiscordService> _logger;

        public DiscordSocketClient Socket { get; }
        public CommandService Commands { get; }

        public DiscordService(
            IServiceProvider services,
            IOptions<AppSettings> options,
            ISet<IDoujinClient> clients,
            MessageFormatter formatter,
            ILoggerFactory loggerFactory,
            IHostingEnvironment environment)
        {
            _services = services;
            _settings = options.Value;
            _clients = clients;
            _formatter = formatter;

            _galleryRegex = new Regex(
                $"({string.Join(")|(", clients.Select(c => c.GalleryRegex))})",
                RegexOptions.Compiled);

            Socket = new DiscordSocketClient(_settings.Discord);
            Commands = new CommandService(_settings.Discord.Command);

            // Register as log provider
            if (environment.IsProduction())
                loggerFactory.AddProvider(new DiscordLogService(this, options));

            _logger = loggerFactory.CreateLogger<DiscordService>();
            _logger.LogDebug($"Gallery match regex: {_galleryRegex}");
        }

        readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public async Task EnsureConnectedAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                await StartSessionAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task StartSessionAsync()
        {
            if (Socket.ConnectionState == ConnectionState.Connected)
                return;

            // Register events
            Socket.MessageReceived += HandleMessageAsyncBackground;

            Socket.Log += HandleLogAsync;
            Commands.Log += HandleLogAsync;

            // Add command modules
            await Commands.AddModulesAsync(typeof(Program).Assembly, _services);

            _formatter.AvailableCommands = Commands.Commands;

            _logger.LogDebug($"Loaded commands: {string.Join(", ", Commands.Commands.Select(c => c.Name))}");

            var connectionSource = new TaskCompletionSource<object>();

            Socket.Ready += handleReady;

            Task handleReady()
            {
                connectionSource.SetResult(null);
                return Task.CompletedTask;
            }

            // Login
            await Socket.LoginAsync(TokenType.Bot, _settings.Discord.Token);
            await Socket.StartAsync();

            // Wait until fully connected
            await connectionSource.Task;

            Socket.Connected -= handleReady;
        }

        public async Task StopSessionAsync()
        {
            // Logout
            await Socket.StopAsync();
            await Socket.LogoutAsync();

            // Unregister events
            Socket.MessageReceived += HandleMessageAsyncBackground;

            Socket.Log -= HandleLogAsync;
            Commands.Log -= HandleLogAsync;
        }

        Task HandleMessageAsyncBackground(SocketMessage message)
        {
            Task.Run(() => HandleMessageAsync(message));
            return Task.CompletedTask;
        }

        async Task HandleMessageAsync(SocketMessage message)
        {
            if (!(message is SocketUserMessage userMessage) ||
                message.Author.Id == Socket.CurrentUser.Id)
                return;

            var argIndex = 0;

            // received message with command prefix
            if (userMessage.HasStringPrefix(_settings.Discord.Prefix, ref argIndex) ||
                userMessage.HasMentionPrefix(Socket.CurrentUser, ref argIndex))
                await ExecuteCommandAsync(userMessage, argIndex);

            // received an arbitrary message
            // scan for gallery URLs and display doujin info
            else await DetectGalleryUrlsAsync(userMessage);
        }

        async Task ExecuteCommandAsync(SocketUserMessage message, int argIndex)
        {
            // command execution
            var context = new SocketCommandContext(Socket, message);
            var result = await Commands.ExecuteAsync(context, argIndex, _services);

            // check for any errors during command execution
            if (result.Error == CommandError.Exception)
            {
                var exception = ((ExecuteResult) result).Exception;

                _logger.LogWarning(exception,
                    $"Exception while handling message {message.Id}: {exception.Message}");

                // notify about this error
                await message.Channel.SendMessageAsync(embed: _formatter.CreateErrorEmbed());
            }
        }

        public delegate Task DoujinDetectHandler(IUserMessage message, IAsyncEnumerable<IDoujin> doujins);

        public event DoujinDetectHandler DoujinsDetected;

        readonly Regex _galleryRegex;

        async Task DetectGalleryUrlsAsync(SocketUserMessage message)
        {
            var content = message.Content;

            // find all recognised gallery urls
            if (!_galleryRegex.IsMatch(content))
                return;

            var doujins = AsyncEnumerable.CreateEnumerable(() =>
            {
                var enumerator = (IEnumerator<Match>) _galleryRegex.Matches(content).GetEnumerator();
                var current = (IDoujin) null;

                return AsyncEnumerable.CreateEnumerator(
                    async token =>
                    {
                        if (!enumerator.MoveNext())
                            return false;

                        var group = enumerator.Current.Groups.First(g =>
                            g.Success &&
                            _clients.Any(c => c.Name == g.Name));

                        current = await _clients
                            .First(c => c.Name == group.Name)
                            .GetAsync(group.Value, token);

                        return true;
                    },
                    () => current,
                    enumerator.Dispose);
            });

            if (DoujinsDetected != null)
                await DoujinsDetected(message, doujins);
        }

        Task HandleLogAsync(LogMessage m)
        {
            LogLevel level;

            switch (m.Severity)
            {
                case LogSeverity.Verbose:
                    level = LogLevel.Trace;
                    break;
                case LogSeverity.Debug:
                    level = LogLevel.Debug;
                    break;
                case LogSeverity.Info:
                    level = LogLevel.Information;
                    break;
                case LogSeverity.Warning:
                    level = LogLevel.Warning;
                    break;
                case LogSeverity.Error:
                    level = LogLevel.Error;
                    break;
                case LogSeverity.Critical:
                    level = LogLevel.Critical;
                    break;
                default:
                    level = LogLevel.None;
                    break;
            }

            if (m.Exception == null)
                _logger.Log(level, m.Message);
            else
                _logger.Log(level, m.Exception, m.Exception.Message);

            return Task.CompletedTask;
        }

        public void Dispose() => Socket.Dispose();
        // Commands.Dispose does not exist
    }
}