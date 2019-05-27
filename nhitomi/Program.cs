using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using nhitomi.Core;

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

            await host.RunAsync();
        }
    }
}