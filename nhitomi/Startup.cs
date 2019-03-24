// Copyright (c) 2018-2019 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using nhitomi.Core;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace nhitomi
{
    public class Startup
    {
        readonly IConfiguration _config;

        public Startup(IConfiguration config)
        {
            _config = config;
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

        public void Configure(IApplicationBuilder app, IHostingEnvironment env) => app.UseMvc();
    }
}