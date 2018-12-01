// Copyright (c) 2018 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PersistentMemoryCache;
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
            .AddSingleton<IMemoryCache>(s => new PersistentMemoryCache.PersistentMemoryCache(
                options: new PersistentMemoryCacheOptions(
                    cacheName: nameof(nhitomi),
                    persistentStore: new LiteDbStore(new LiteDbOptions(
                        fileName: $"nhitomi_cache.db"
                    ))
                )
            ))

            // Logging
            .AddLogging(
                logging => logging
                    .AddConfiguration(Configuration.GetSection("logging"))
                    .AddConsole()
            )

            // Formatters
            .AddTransient<JsonSerializer>(s => JsonSerializer.CreateDefault())

            // Discord
            .AddSingleton<DiscordService>()
            .AddSingleton<InteractiveScheduler>()

            // Doujin clients
            .AddSingleton<nhentaiClient>()
            .AddSingleton<HitomiClient>()
            .AddSingleton<ISet<IDoujinClient>>(s => new HashSet<IDoujinClient>
            {
                s.GetService<nhentaiClient>().Synchronized(),
                s.GetService<HitomiClient>().Synchronized()
            })

            // Background services
            .AddSingleton<StatusUpdater>()
            .AddSingleton<DoujinClientUpdater>()
            .AddSingleton<DownloadServer>()
            .AddSingleton<ISet<IBackgroundService>>(s => new HashSet<IBackgroundService>
            {
                s.GetService<StatusUpdater>(),
                s.GetService<DoujinClientUpdater>(),
                s.GetService<DownloadServer>()
            });
    }
}