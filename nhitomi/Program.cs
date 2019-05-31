using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using nhitomi.Core;
using nhitomi.Discord;

namespace nhitomi
{
    public static class Program
    {
        static async Task Main()
        {
            // build host
            using (var host = new HostBuilder()
                .UseContentRoot(Environment.CurrentDirectory)
                .UseEnvironment(Environment.GetEnvironmentVariable("ENVIRONMENT") ?? EnvironmentName.Development)
                .ConfigureAppConfiguration(Startup.Configure)
                .ConfigureServices(Startup.ConfigureServices)
                .Build())
            {
                // initialization
                using (var scope = host.Services.CreateScope())
                    await DependencyUtility<Initialization>.Factory(scope.ServiceProvider).RunAsync();

                // run host
                await host.RunAsync();
            }
        }

        sealed class Initialization
        {
            readonly IHostingEnvironment _environment;
            readonly nhitomiDbContext _db;
            readonly DiscordService _discord;
            readonly IApiClient _apiClient;

            public Initialization(IHostingEnvironment environment, nhitomiDbContext db, DiscordService discord,
                IApiClient apiClient)
            {
                _environment = environment;
                _db = db;
                _discord = discord;
                _apiClient = apiClient;
            }

            public async Task RunAsync(CancellationToken cancellationToken = default)
            {
                // login to api
                await _apiClient.LoginAsync(cancellationToken);

                // migrate local database in development
                if (_environment.IsDevelopment())
                    await _db.Database.MigrateAsync(cancellationToken);

                // start discord
                await _discord.ConnectAsync();
            }
        }
    }
}