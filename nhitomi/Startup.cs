using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using nhitomi.Core;
using nhitomi.Discord;
using nhitomi.Interactivity;
using Newtonsoft.Json;

namespace nhitomi
{
    public static class Startup
    {
        public static void Configure(HostBuilderContext host,
                                     IConfigurationBuilder config)
        {
            config
               .SetBasePath(host.HostingEnvironment.ContentRootPath);

            config
               .AddJsonFile("appsettings.json")
               .AddJsonFile($"appsettings.{host.HostingEnvironment.EnvironmentName}.json", true);

            config
               .AddEnvironmentVariables();
        }

        public static void ConfigureServices(HostBuilderContext host,
                                             IServiceCollection services)
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
               .AddSingleton<DiscordErrorReporter>()
               .AddHostedInjectableService<MessageHandlerService>()
               .AddHostedInjectableService<ReactionHandlerService>()
               .AddHostedInjectableService<StatusUpdateService>()
               .AddHostedInjectableService<LogHandlerService>()
               .AddHostedInjectableService<GuildSettingsSyncService>()
               .AddHostedInjectableService<FeedChannelUpdateService>()
               .AddHostedInjectableService<GuildWelcomeMessageService>();

            // other stuff
            services
               .AddHttpClient()
               .AddTransient<IHttpClient, HttpClientWrapper>()
               .AddTransient(s => JsonSerializer.Create(new nhitomiSerializerSettings()))
               .AddHostedInjectableService<ForcedGarbageCollector>();
        }
    }

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHostedInjectableService<TService>(this IServiceCollection collection)
            where TService : class, IHostedService => collection
                                                     .AddSingleton<TService>()
                                                     .AddSingleton<IHostedService, TService>(
                                                          s => s.GetService<TService>());
    }
}