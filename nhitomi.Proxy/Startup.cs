// Copyright (c) 2018-2019 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using nhitomi.Core;
using Newtonsoft.Json;

namespace nhitomi.Proxy
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
                .AddCors(c => c
                    .AddPolicy("DefaultPolicy", p => p
                        .WithOrigins("https://nhitomi.herokuapp.com")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()));

            services
                // Configuration
                .Configure<AppSettings>(_config)

                // HTTP client
                .AddHttpClient()

                // Formatters
                .AddTransient(s => JsonSerializer.Create(new nhitomiSerializerSettings()));
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env) => app
            .UseCors("DefaultPolicy")
            .UseMvc();
    }
}