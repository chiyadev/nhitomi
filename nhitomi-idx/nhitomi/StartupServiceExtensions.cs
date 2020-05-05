using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

        public static IServiceCollection AddInjectableHostedService<TService>(this IServiceCollection collection) where TService : class, IHostedService
            => collection.AddSingleton<TService>()
                         .AddTransient<IHostedService>(s => s.GetService<TService>()); // transient because https://github.com/dotnet/extensions/issues/553#issuecomment-404547620

        public static IServiceCollection AddInjectableHostedService<TService, TImplementation>(this IServiceCollection collection) where TService : class where TImplementation : class, TService, IHostedService
            => collection.AddSingleton<TImplementation>()
                         .AddSingleton<TService, TImplementation>(s => s.GetService<TImplementation>())
                         .AddTransient<IHostedService>(o => o.GetService<TImplementation>());
    }
}