// Copyright (c) 2018 phosphene47
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
using System.Threading.Tasks;

namespace nhitomi
{
    public class DiscordService : IDisposable
    {
        readonly IServiceProvider _services;
        readonly AppSettings _settings;
        readonly ISet<IDoujinClient> _clients;
        readonly InteractiveScheduler _interactive;
        readonly ILogger _logger;

        public DiscordSocketClient Socket { get; }
        public CommandService Commands { get; }

        public DiscordService(
            IServiceProvider services,
            IOptions<AppSettings> options,
            ISet<IDoujinClient> clients,
            InteractiveScheduler interactive,
            ILoggerFactory loggerFactory
        )
        {
            _services = services;
            _settings = options.Value;
            _clients = clients;
            _interactive = interactive;

            _galleryRegex = new Regex(
                pattern: $"({string.Join(")|(", clients.Select(c => c.GalleryRegex))})",
                options: RegexOptions.Compiled
            );

            Socket = new DiscordSocketClient(_settings.Discord);
            Commands = new CommandService(_settings.Discord.Command);

            // Register as log provider
            if (_settings.CurrentEnvironment == "PRODUCTION")
                loggerFactory.AddProvider(new DiscordLogRedirector(options, this));

            _logger = loggerFactory.CreateLogger<DiscordService>();
        }

        public async Task StartSessionAsync()
        {
            // Register events
            Socket.Log += handleLogAsync;
            Socket.MessageReceived += handleMessageReceivedAsync;
            Commands.Log += handleLogAsync;

            Socket.ReactionAdded += _interactive.HandleReaction;
            Socket.ReactionRemoved += _interactive.HandleReaction;

            // Add modules
            await Commands.AddModulesAsync(typeof(Program).Assembly);

            // Login
            await Socket.LoginAsync(TokenType.Bot, _settings.Discord.Token);
            await Socket.StartAsync();
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
                                var executionResult = (ExecuteResult)result;
                                throw executionResult.Exception;
                        }
                }
                else
                {
                    // Find all recognised gallery urls and disply info
                    var matches = _galleryRegex
                        .Matches(userMessage.Content)
                        .Cast<Match>();

                    if (!matches.Any())
                        return;

                    var response = await userMessage.Channel.SendMessageAsync(
                        text: "**nhitomi**: Loading..."
                    );

                    var results = AsyncEnumerable.CreateEnumerable(() =>
                    {
                        var enumerator = matches.GetEnumerator();
                        var current = (IDoujin)null;

                        return AsyncEnumerable.CreateEnumerator(
                            moveNext: async token =>
                            {
                                if (!enumerator.MoveNext())
                                    return false;

                                var group = enumerator.Current.Groups.First(g =>
                                    g.Success &&
                                    _clients.Any(c => c.Name.Equals(g.Name, StringComparison.OrdinalIgnoreCase)));

                                current = await _clients
                                    .First(c => c.Name.Equals(group.Name, StringComparison.OrdinalIgnoreCase))
                                    .GetAsync(group.Value);
                                return true;
                            },
                            current: () => current,
                            dispose: enumerator.Dispose
                        );
                    });

                    await DoujinModule.DisplayListAsync(
                        request: userMessage,
                        response: response,
                        results: results,
                        interactive: _interactive
                    );
                }
            }
            catch (Exception e)
            {
                // Log
                _logger.LogWarning(e, $"Caught exception while handling message {userMessage.Id}: {e.Message}");

                // Send error message
                await userMessage.Channel.SendMessageAsync(
                    text: string.Empty,
                    embed: MessageFormatter.EmbedError()
                );
            }
        }

        Task handleLogAsync(LogMessage m)
        {
            var level = LogLevel.None;

            switch (m.Severity)
            {
                case LogSeverity.Verbose: level = LogLevel.Trace; break;
                case LogSeverity.Debug: level = LogLevel.Debug; break;
                case LogSeverity.Info: level = LogLevel.Information; break;
                case LogSeverity.Warning: level = LogLevel.Warning; break;
                case LogSeverity.Error: level = LogLevel.Error; break;
                case LogSeverity.Critical: level = LogLevel.Critical; break;
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