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
            var host = new HostBuilder()
                .ConfigureAppConfiguration(Startup.Configure)
                .ConfigureServices(Startup.ConfigureServices)
                .Build();

            var environment = host.Services.GetRequiredService<IHostingEnvironment>();

            // migrate database
            if (environment.IsDevelopment())
            {
                using (var scope = host.Services.CreateScope())
                    await scope.ServiceProvider.GetRequiredService<nhitomiDbContext>().Database.MigrateAsync();
            }

            var discord = host.Services.GetRequiredService<DiscordService>();

            // connect discord
            await discord.ConnectAsync();

            try
            {
                // run host
                await host.RunAsync();
            }
            finally
            {
                // disconnect discord
                await discord.DisconnectAsync();
            }
        }
    }
}