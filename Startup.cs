using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace nhitomi
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; set; }

        public Assembly Assembly => typeof(Program).Assembly;

        public void ConfigureServices(IServiceCollection services) => services
            // Configuration
            .Configure<AppSettings>(Configuration)

            // Program
            .AddSingleton<Program>()

            // HTTP client
            .AddHttpClient()

            // Caching
            .AddTransient<IMemoryCache, MemoryCache>()

            // Logging
            .AddLogging(
                logging => logging
                    .AddConfiguration(Configuration.GetSection("logging"))
                    .AddConsole()
            )

            // Formatters
            .AddTransient<JsonSerializer>(s => JsonSerializer.CreateDefault())
            .AddTransient<MessageFormatter>()

            // Discord
            .AddSingleton<DiscordService>()
            .AddSingleton<InteractiveScheduler>()

            // Doujin clients
            .AddSingleton<nhentaiClient>()
            .AddSingleton<HitomiClient>()
            .AddSingleton<ISet<IDoujinClient>>(s => new HashSet<IDoujinClient>
            {
                s.GetService<nhentaiClient>(),
                s.GetService<HitomiClient>()
            })

            // Background services
            .AddSingleton<StatusUpdater>()
            .AddSingleton<DoujinClientUpdater>()
            .AddSingleton<ISet<IBackgroundService>>(s => new HashSet<IBackgroundService>
            {
                s.GetService<StatusUpdater>(),
                s.GetService<DoujinClientUpdater>()
            });
    }
}