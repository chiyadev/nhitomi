// Copyright (c) 2018-2019 fate/loli
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using nhitomi.Core;
using nhitomi.Proxy.Services;
using Newtonsoft.Json;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace nhitomi.Proxy
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
                .AddJsonFormatters(nhitomiSerializerSettings.Apply)
                .AddCors(c => c
                    .AddPolicy("DefaultPolicy", p => p
                        .WithOrigins(_config.Get<AppSettings>().Http.MainServerUrl)
                        .AllowAnyHeader()
                        .AllowAnyMethod()));

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
                .AddSingleton<CacheSyncService>()
                .AddSingleton<IHostedService, CacheSyncService>(s => s.GetRequiredService<CacheSyncService>())
                .AddHostedService<ProxyRegistrationService>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseStatusCodePages();

            if (env.IsProduction())
                app.UseHttpsRedirection();

            app.UseCors("DefaultPolicy")
                .UseMvc();
        }
    }
}