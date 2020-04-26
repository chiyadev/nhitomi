using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using nhitomi.Scrapers;

namespace nhitomi
{
    static class StartupServiceExtensions
    {
        public static IMvcCoreBuilder AddTestControllers(this IMvcCoreBuilder builder)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "nhitomi.Tests.dll");

            if (File.Exists(path))
                builder = builder.AddApplicationPart(Assembly.LoadFrom(path));

            return builder;
        }

        public static IServiceCollection AddInjectableHostedService<T>(this IServiceCollection collection) where T : class, IHostedService
            => collection.AddSingleton<T>()
                         .AddTransient<IHostedService>(o => o.GetService<T>()); // transient because https://github.com/dotnet/extensions/issues/553#issuecomment-404547620

        public static IServiceCollection AddScraper<T>(this IServiceCollection collection) where T : class, IScraper
            => collection.AddInjectableHostedService<T>()
                         .AddSingleton<IScraper>(o => o.GetService<T>());
    }
}