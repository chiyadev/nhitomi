// Copyright (c) 2018-2019 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Collections.Generic;
using nhitomi.Core;

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
                .AddJsonFormatters(nhitomiSerializerSettings.Apply)
                .AddCors();

            services
                // Configuration
                .Configure<AppSettings>(_config)

                // Program
                .AddSingleton<Program>()

                // HTTP client
                .AddHttpClient()

                // Formatters
                .AddTransient(s => JsonSerializer.Create(new nhitomiSerializerSettings()))

                // Discord
                .AddSingleton<DiscordService>()
                .AddSingleton<InteractiveScheduler>()
                .AddHostedService<StatusUpdater>()
                .AddHostedService<FeedUpdater>()

                // Doujin clients
                .AddSingleton<nhentaiHtmlClient>()
                .AddSingleton<HitomiClient>()
                // .AddSingleton<TsuminoClient>()
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
