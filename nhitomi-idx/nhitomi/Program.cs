using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Writers;
using nhitomi.Database;
using nhitomi.Database.Migrations;
using Swashbuckle.AspNetCore.Swagger;

namespace nhitomi
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            using var host = CreateWebHostBuilder(args).Build();

            if (!await HandleArgsAsync(host, args))
            {
                await host.Services.GetService<StartupInitializer>().RunAsync();
                await host.RunAsync();
            }
        }

        static async Task<bool> HandleArgsAsync(IWebHost host, IEnumerable<string> args)
        {
            foreach (var arg in args)
            {
                switch (arg)
                {
                    // generate API specification
                    case "--generate-spec":
                    {
                        var link    = host.Services.GetService<ILinkGenerator>();
                        var swagger = host.Services.GetService<ISwaggerProvider>();

                        swagger.GetSwagger("docs", link.GetApiLink("/")).SerializeAsV3(new OpenApiJsonWriter(Console.Out));
                        return true;
                    }

                    // run migrations
                    case "--migrations":
                    {
                        var manager = host.Services.GetService<IMigrationManager>();

                        await manager.RunAsync();
                        return true;
                    }
                }
            }

            return false;
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder<Startup>(args)
                   .UseContentRoot(AppContext.BaseDirectory)
                   .UseWebRoot(Path.Combine(AppContext.BaseDirectory, "static"))
                   .ConfigureAppConfiguration(config =>
                    {
                        config.AddJsonFile("appsettings.Local.json", true, true);

                        config.Add(new ElasticConfigurationSource());
                    });
    }
}