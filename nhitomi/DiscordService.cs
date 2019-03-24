// Copyright (c) 2018-2019 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using nhitomi.Core;
using Newtonsoft.Json;

namespace nhitomi
{
    public class DiscordService : IDisposable
    {
        readonly IServiceProvider _services;
        readonly AppSettings _settings;
        readonly ISet<IDoujinClient> _clients;
        readonly InteractiveScheduler _interactive;
        readonly JsonSerializer _json;
        readonly ILogger _logger;

        public DiscordSocketClient Socket { get; }
        public CommandService Commands { get; }

        public DiscordService(
            IServiceProvider services,
            IOptions<AppSettings> options,
            ISet<IDoujinClient> clients,
            InteractiveScheduler interactive,
            JsonSerializer json,
            ILoggerFactory loggerFactory
        )
        {
            _services = services;
            _settings = options.Value;
            _clients = clients;
            _interactive = interactive;
            _json = json;

            _galleryRegex = new Regex(
                $"({string.Join(")|(", clients.Select(c => c.GalleryRegex))})",
                RegexOptions.Compiled
            );

            Socket = new DiscordSocketClient(_settings.Discord);
            Commands = new CommandService(_settings.Discord.Command);

            // Register as log provider
            if (_settings.CurrentEnvironment == "PRODUCTION")
                loggerFactory.AddProvider(new DiscordLogService(this));

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
            Socket.Log += handleLogAsync;
            Socket.MessageReceived += handleMessageReceivedAsync;
            Commands.Log += handleLogAsync;

            Socket.ReactionAdded += _interactive.HandleReaction;
            Socket.ReactionRemoved += _interactive.HandleReaction;

            // Add modules
            await Commands.AddModulesAsync(typeof(Program).Assembly, _services);

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

            _interactive.IgnoreReactionUsers.Clear();
            _interactive.IgnoreReactionUsers.Add(Socket.CurrentUser.Id);
        }

        public async Task StopSessionAsync()
        {
            // Logout
            await Socket.StopAsync();
            await Socket.LogoutAsync();

            // Unregister events
            Socket.ReactionAdded -= _interactive.HandleReaction;
            Socket.ReactionRemoved -= _interactive.HandleReaction;

            Socket.Log -= handleLogAsync;
            Socket.MessageReceived += handleMessageReceivedAsync;
            Commands.Log -= handleLogAsync;
        }

        readonly Regex _galleryRegex;

        async Task handleMessageReceivedAsync(SocketMessage message)
        {
            if (!(message is SocketUserMessage userMessage) ||
                message.Author.Id == Socket.CurrentUser.Id)
                return;

            try
            {
                var argIndex = 0;

                if (userMessage.HasStringPrefix(_settings.Prefix, ref argIndex) ||
                    userMessage.HasMentionPrefix(Socket.CurrentUser, ref argIndex))
                {
                    // Execute command
                    var context = new SocketCommandContext(Socket, userMessage);
                    var result = await Commands.ExecuteAsync(context, argIndex, _services);

                    if (result.Error.HasValue)
                        switch (result.Error.Value)
                        {
                            case CommandError.Exception:
                                var executionResult = (ExecuteResult) result;
                                throw executionResult.Exception;
                        }
                }
                else
                {
                    // Find all recognised gallery urls and display info
                    var matches = _galleryRegex
                        .Matches(userMessage.Content)
                        .ToArray();

                    if (matches.Length == 0)
                        return;

                    var response = await userMessage.Channel.SendMessageAsync("**nhitomi**: Loading...");

                    var results = AsyncEnumerable.CreateEnumerable(() =>
                    {
                        var enumerator = ((IEnumerable<Match>) matches).GetEnumerator();
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
                                    .Single(c => c.Name == group.Name)
                                    .GetAsync(group.Value);

                                return true;
                            },
                            () => current,
                            enumerator.Dispose
                        );
                    });

                    await DoujinModule.DisplayListAsync(
                        userMessage, response, results, _interactive, Socket, _json, _settings);
                }
            }
            catch (Exception e)
            {
                // Log
                _logger.LogWarning(e, $"Exception while handling message {userMessage.Id}: {e.Message}");

                // Send error message
                await userMessage.Channel.SendMessageAsync(string.Empty, embed: MessageFormatter.EmbedError());
            }
        }

        Task handleLogAsync(LogMessage m)
        {
            var level = LogLevel.None;

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
            }

            if (m.Exception == null)
                _logger.Log(level, m.Message);
            else
                _logger.Log(level, m.Exception, m.Exception.Message);

            return Task.CompletedTask;
        }

        public void Dispose() => Socket.Dispose();
    }
}