using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

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
    }
}