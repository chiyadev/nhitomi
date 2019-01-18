// Copyright (c) 2018 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using Discord.Commands;
using Discord.WebSocket;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace nhitomi
{
    public class Startup
    {
        readonly IConfiguration _config;

        public Startup(IConfiguration config)
        {
            _config = config;
        }

        public Assembly Assembly => typeof(Program).Assembly;

        public void ConfigureServices(IServiceCollection services)
        {
            services
                // Framework
                .AddMvcCore()
                .AddFormatterMappings()
                .AddJsonFormatters()
                .AddCors();

            services
                // Configuration
                .Configure<AppSettings>(_config)

                // Program
                .AddSingleton<Program>()

                // HTTP client
                .AddHttpClient()

                // Caching
                .AddSingleton<IMemoryCache>(s => new MemoryCache(new MemoryCacheOptions
                {
                    ExpirationScanFrequency = TimeSpan.FromMinutes(1)
                }))

                // Formatters
                .AddTransient<JsonSerializer>(s => new nhitomiJsonSerializer())

                // Discord
                .AddSingleton<DiscordService>()
                .AddSingleton<InteractiveScheduler>()
                .AddHostedService<StatusUpdater>()
                .AddHostedService<DoujinClientUpdater>()

                // Doujin clients
                .AddSingleton<nhentaiHtmlClient>()
                .AddSingleton<HitomiClient>()
                .AddSingleton<TsuminoClient>()
                .AddSingleton<ISet<IDoujinClient>>(s => new HashSet<IDoujinClient>
                {
                    s.GetService<nhentaiHtmlClient>().Synchronized(),
                    s.GetService<HitomiClient>().Synchronized(),
                    s.GetService<TsuminoClient>().Synchronized()
                });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env) => app.UseMvc();
    }
}