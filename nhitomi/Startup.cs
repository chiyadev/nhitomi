using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using nhitomi.Core;
using nhitomi.Database;
using nhitomi.Http;
using nhitomi.Services;
using Newtonsoft.Json;

namespace nhitomi
{
    public static class Startup
    {
        public static void Configure(HostBuilderContext host, IConfigurationBuilder config)
        {
            config
                .SetBasePath(host.HostingEnvironment.ContentRootPath)
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile($"appsettings.{host.HostingEnvironment.EnvironmentName}.json", true)
                .AddEnvironmentVariables();
        }

        public static void ConfigureServices(HostBuilderContext host, IServiceCollection services)
        {
            // configuration
            services
                .Configure<AppSettings>(host.Configuration);

            // logging
            services
                .AddLogging(l => l.AddConfiguration(host.Configuration.GetSection("logging"))
                    .AddConsole()
                    .AddDebug());

            // discord services
            services
                .AddSingleton<DiscordService>()
                .AddSingleton<InteractiveManager>()
                .AddSingleton<MessageFormatter>()
                .AddHostedService<StatusUpdater>()
                .AddHostedService<FeedUpdater>();

            // database
            services
                .AddSingleton<IDatabase, DynamoDbDatabase>();

            // other stuff
            services
                .AddHttpClient()
                .AddTransient(s => JsonSerializer.Create(new nhitomiSerializerSettings()));

            // http server
            services
                .AddHostedService<HttpService>()
                .AddSingleton<ProxyService>();
        }
    }
}