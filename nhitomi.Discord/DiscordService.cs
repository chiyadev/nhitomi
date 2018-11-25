using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace nhitomi
{
    public class DiscordService
    {
        readonly IServiceProvider _services;
        readonly AppSettings _settings;
        readonly ILogger _logger;

        public DiscordSocketClient Socket { get; }
        public CommandService Commands { get; }

        public DiscordService(
            IServiceProvider services,
            IOptions<AppSettings> options,
            ILoggerFactory loggerFactory
        )
        {
            _services = services;
            _settings = options.Value;

            Socket = new DiscordSocketClient(_settings.Discord);
            Commands = new CommandService(_settings.Discord.Command);

            // Register as log provider
            loggerFactory.AddProvider(new DiscordLogRedirector(options, this));
            _logger = loggerFactory.CreateLogger<DiscordService>();
        }

        public async Task StartSessionAsync()
        {
            // Register events
            Socket.Log += handleLogAsync;
            Socket.MessageReceived += handleMessageReceivedAsync;
            Commands.Log += handleLogAsync;

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
            Socket.Log -= handleLogAsync;
            Socket.MessageReceived += handleMessageReceivedAsync;
            Commands.Log -= handleLogAsync;
        }

        async Task handleMessageReceivedAsync(SocketMessage message)
        {
            if (!(message is SocketUserMessage userMessage) ||
                message.Author.Id == Socket.CurrentUser.Id)
                return;

            var argIndex = 0;

            if (userMessage.HasStringPrefix(_settings.Prefix, ref argIndex) ||
                userMessage.HasMentionPrefix(Socket.CurrentUser, ref argIndex))
            {
                // Execute command
                var context = new SocketCommandContext(Socket, userMessage);
                var result = await Commands.ExecuteAsync(context, argIndex, _services);

                // If not successful, reply with error
                if (!result.IsSuccess)
                    await context.Channel.SendMessageAsync(result.ToString());
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
    }
}