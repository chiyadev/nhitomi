using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using nhitomi.Core;
using nhitomi.Database;
using nhitomi.Services;
using Newtonsoft.Json;

namespace nhitomi
{
    public static class Program
    {
        static Task Main(string[] args) =>
            new HostBuilder()
                .ConfigureAppConfiguration((host, config) =>
                {
                    config
                        .SetBasePath(host.HostingEnvironment.ContentRootPath)
                        .AddJsonFile("appsettings.json", false)
                        .AddJsonFile($"appsettings.{host.HostingEnvironment.EnvironmentName}.json", true)
                        .AddEnvironmentVariables();
                })
                .ConfigureServices((host, services) =>
                {
                    services
                        // configuration
                        .Configure<AppSettings>(host.Configuration)

                        // logging
                        .AddLogging(l => l
                            .AddConfiguration(host.Configuration.GetSection("logging"))
                            .AddConsole()
                            .AddDebug())

                        // background services
                        .AddSingleton<DiscordService>()
                        .AddHostedService<StatusUpdater>()
                        .AddHostedService<FeedUpdater>()
                        .AddSingleton<InteractiveManager>()
                        .AddHostedService<ProxyListBroadcastService>()
                        .AddSingleton<MessageFormatter>()

                        // database
                        .AddSingleton<IDatabase, DynamoDbDatabase>()

                        // other stuff
                        .AddHttpClient()
                        .AddTransient(s => JsonSerializer.Create(new nhitomiSerializerSettings()));
                })
                .RunConsoleAsync();
    }
}