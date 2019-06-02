using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nhitomi.Discord.Parsing;

namespace nhitomi.Discord
{
    public class CommandExecutor : IMessageHandler
    {
        readonly IServiceProvider _services;
        readonly AppSettings _settings;
        readonly ILogger<CommandExecutor> _logger;

        public CommandExecutor(IServiceProvider services, IOptions<AppSettings> options,
            ILogger<CommandExecutor> logger)
        {
            _services = services;
            _settings = options.Value;
            _logger = logger;
        }

        readonly List<CommandInfo> _commands = new List<CommandInfo>();

        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            // load commands
            _commands.AddRange(typeof(Startup).Assembly
                .GetTypes()
                .Where(t => !t.IsAbstract && t.IsClass)
                .SelectMany(t => t.GetMethods())
                .Where(t => t.GetCustomAttribute<CommandAttribute>() != null)
                .OrderBy(t => t.Name)
                .ThenByDescending(t => t.GetParameters().Length) // prioritize specific commands
                .Select(t => new CommandInfo(t)));

            _logger.LogDebug($"Loaded commands: {string.Join(", ", _commands.Select(c => c.Attribute.Name))}");

            return Task.CompletedTask;
        }

        bool TryParseCommand(string str, out CommandInfo command, out Dictionary<string, object> args)
        {
            foreach (var c in _commands)
                if (c.TryParse(str, out args))
                {
                    command = c;
                    return true;
                }

            command = null;
            args = null;
            return false;
        }

        public async Task<bool> TryHandleAsync(IMessageContext context, CancellationToken cancellationToken = default)
        {
            switch (context.Event)
            {
                case MessageEvent.Create:
                    break;

                default:
                    return false;
            }

            var content = context.Message.Content;

            // message has command prefix
            if (!content.StartsWith(_settings.Discord.Prefix))
                return false;

            content = content.Substring(_settings.Discord.Prefix.Length);

            // parse command
            if (!TryParseCommand(content, out var command, out var args))
                return false;

            using (var scope = _services.CreateScope())
            {
                var services = new ServiceDictionary(scope.ServiceProvider)
                {
                    {typeof(IDiscordContext), context},
                    {typeof(IMessageContext), context}
                };

                // invoke command
                using (context.BeginTyping())
                    await command.InvokeAsync(services, args);
            }

            return true;
        }
    }
}