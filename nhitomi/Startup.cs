// Copyright (c) 2018-2019 fate/loli
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using nhitomi.Core;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace nhitomi
{
    public class Startup
    {
        readonly IConfiguration _config;

        public Startup(IHostingEnvironment environment)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(environment.ContentRootPath)
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile($"appsettings.{environment.EnvironmentName}.json", true)
                .AddEnvironmentVariables();

            _config = builder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                // Framework
                .AddMvcCore()
                .AddFormatterMappings()
                .AddJsonFormatters(nhitomiSerializerSettings.Apply);

            services
                // Configuration
                .Configure<AppSettings>(_config)

                // Logging
                .AddLogging(l => l
                    .AddConfiguration(_config.GetSection("logging"))
                    .AddConsole()
                    .AddDebug())

                // HTTP client
                .AddHttpClient()

                // Formatters
                .AddTransient(s => JsonSerializer.Create(new nhitomiSerializerSettings()))

                // Services
                .AddSingleton<DiscordService>()
                .AddSingleton<InteractiveScheduler>()
                .AddHostedService<StatusUpdater>()
                .AddHostedService<FeedUpdater>()
                .AddSingleton<DownloadProxyManager>()
                .AddSingleton<IHostedService, DownloadProxyManager>(p => p.GetRequiredService<DownloadProxyManager>())

                // Doujin clients
                .AddSingleton<nhentaiHtmlClient>()
                .AddSingleton<HitomiClient>()
                .AddSingleton<TsuminoClient>()
                .AddSingleton<PururinClient>()
                .AddSingleton<ISet<IDoujinClient>>(s => new HashSet<IDoujinClient>
                {
                    s.GetService<nhentaiHtmlClient>().Synchronized(),
                    s.GetService<HitomiClient>().Synchronized(),
                    // s.GetService<TsuminoClient>().Synchronized(),
                    // s.GetService<PururinClient>().Synchronized()
                });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsProduction())
                app.UseHttpsRedirection();

            app.UseMvc();
        }
    }
}