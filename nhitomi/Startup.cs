using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using nhitomi.Core;
using nhitomi.Discord;
using nhitomi.Http;
using nhitomi.Interactivity;
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

            // database
            services
                .AddScoped<IDatabase>(s => s.GetRequiredService<nhitomiDbContext>())
                .AddDbContextPool<nhitomiDbContext>(d => d
                    .UseMySql(host.Configuration.GetConnectionString("nhitomi")));

            // discord services
            services
                .AddSingleton<DiscordService>()
                .AddSingleton<CommandExecutor>()
                .AddSingleton<GalleryUrlDetector>()
                .AddSingleton<InteractiveManager>()
                .AddSingleton<GuildSettingsCache>()
                .AddSingleton<MessageHandlerService>()
                .AddSingleton<ReactionHandlerService>()
                .AddSingleton<StatusUpdateService>()
                .AddSingleton<LogHandlerService>()
                .AddSingleton<GuildSettingsSyncService>()
                .AddSingleton<FeedChannelUpdateService>()
                .AddSingleton<IHostedService, MessageHandlerService>(s => s.GetService<MessageHandlerService>())
                .AddSingleton<IHostedService, ReactionHandlerService>(s => s.GetService<ReactionHandlerService>())
                .AddSingleton<IHostedService, StatusUpdateService>(s => s.GetService<StatusUpdateService>())
                .AddSingleton<IHostedService, LogHandlerService>(s => s.GetService<LogHandlerService>())
                .AddSingleton<IHostedService, GuildSettingsSyncService>(s => s.GetService<GuildSettingsSyncService>())
                .AddSingleton<IHostedService, FeedChannelUpdateService>(s => s.GetService<FeedChannelUpdateService>());

            // http server
            services
                .AddSingleton<HttpService>()
                .AddSingleton<ProxyHandler>()
                .AddSingleton<IHostedService, HttpService>(s => s.GetService<HttpService>());

            // other stuff
            services
                .AddHttpClient()
                .AddTransient<IHttpClient, HttpClientWrapper>()
                .AddTransient(s => JsonSerializer.Create(new nhitomiSerializerSettings()))
                .AddHostedService<ForcedGarbageCollector>();
        }
    }
}