// Copyright (c) 2018-2019 chiya.dev
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nhitomi.Core;
using nhitomi.Core.Clients;
using nhitomi.Database;
using nhitomi.Services;
using Newtonsoft.Json;

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
                .AddHostedService<StatusUpdater>()
                .AddHostedService<FeedUpdater>()
                .AddSingleton<InteractiveManager>()
                .AddSingleton<ProxyList>()
                .AddHostedService<ProxyListBroadcastService>()
                .AddSingleton<MessageFormatter>()
                .AddSingleton<IHttpProxyClient, HttpProxyClient>()

                // Doujin clients
                .AddSingleton<nhentaiHtmlClient>()
                .AddSingleton<HitomiClient>()
                //.AddSingleton<TsuminoClient>()
                //.AddSingleton<PururinClient>()
                .AddSingleton<ISet<IDoujinClient>>(s => new IDoujinClient[]
                    {
                        s.GetRequiredService<nhentaiHtmlClient>(),
                        s.GetRequiredService<HitomiClient>(),
                        //s.GetRequiredService<TsuminoClient>(),
                        //s.GetRequiredService<PururinClient>()
                    }
                    //.Select(c => c.Synchronized())
                    //.Select(c => c.Filtered())
                    .ToHashSet())

                // Databases
                .AddSingleton<IDatabase, DynamoDbDatabase>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseStatusCodePages();

            if (env.IsProduction())
                app.UseHttpsRedirection();

            app.UseMvc();
        }
    }
}