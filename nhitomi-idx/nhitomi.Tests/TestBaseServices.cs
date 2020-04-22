using System;
using System.Threading.Tasks;
using ChiyaFlake;
using Elasticsearch.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using nhitomi.Controllers;
using nhitomi.Database;
using nhitomi.Storage;
using NUnit.Framework;

namespace nhitomi
{
    [Parallelizable(ParallelScope.Self)]
    public abstract class TestBaseServices
    {
        IServiceScope _serviceScope;

        protected IServiceProvider Services => _serviceScope.ServiceProvider;

        [SetUp]
        public virtual async Task SetUpAsync()
        {
            _serviceScope = SetUpServices().CreateScope();

            // startup initializer
            await Services.GetService<StartupInitializer>().RunAsync();
        }

        [TearDown]
        public virtual async Task TearDownAsync()
        {
            // reset storage
            Services.GetService<IStorage>().Dispose();

            // reset elasticsearch
            await Services.GetService<IElasticClient>().ResetAsync();

            // reset redis
            await Services.GetService<IRedisClient>().ResetAsync();

            // dispose services
            _serviceScope.Dispose();

            _services?.Dispose();
        }

        ServiceProvider _services;

        protected virtual IServiceProvider SetUpServices()
        {
            var services      = new ServiceCollection();
            var configuration = new ConfigurationBuilder().Build();
            var environment   = new DummyHostingEnvironment();

            // add webhost services
            services.AddSingleton<IWebHostEnvironment>(environment)
                    .AddSingleton<IHostEnvironment>(environment)
                    .AddSingleton<IConfiguration>(configuration)
                    .AddLogging(l => l.SetMinimumLevel(LogLevel.Trace)
                                      .AddConsole());

            // startup services
            new Startup(configuration, environment).ConfigureServices(services);

            ConfigureServices(services);

            return _services = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = true
            });
        }

        protected virtual void ConfigureServices(IServiceCollection services)
        {
            var prefix = Snowflake.New.ToLowerInvariant();

            // use in-memory storage
            services.PostConfigure<StorageOptions>(o => o.Type = StorageType.Memory);

            // don't bother first admin user
            services.PostConfigure<UserServiceOptions>(o => o.FirstUserAdmin = false);

            services.PostConfigure<ElasticOptions>(o =>
            {
                // use a random elasticsearch index prefix to avoid clashing between parallel tests
                o.IndexPrefix = $"nhitomi-test-{prefix}-";

                // make elasticsearch refresh immediately to make tests run faster
                o.RequestRefreshOption = Refresh.True;

                // disable dynamic config to make tests faster
                o.EnableDynamicConfig = false;
            });

            // use a random redis key prefix
            services.PostConfigure<RedisOptions>(o => o.KeyPrefix = $"{prefix}:");
        }

        sealed class DummyHostingEnvironment : IWebHostEnvironment
        {
            public string ApplicationName { get; set; } = "nhitomi";
            public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Environment.CurrentDirectory);
            public string ContentRootPath { get; set; } = Environment.CurrentDirectory;
            public string EnvironmentName { get; set; } = Environments.Development;
            public IFileProvider WebRootFileProvider { get; set; } = new PhysicalFileProvider(Environment.CurrentDirectory);
            public string WebRootPath { get; set; } = Environment.CurrentDirectory;
        }

        protected Task<DbUser> MakeUserAsync(string name = null, Action<DbUser> configure = null)
        {
            var client = Services.GetService<IElasticClient>();

            var entry = client.Entry(new DbUser
            {
                Username = name ?? "user"
            });

            configure?.Invoke(entry.Value);

            return entry.CreateAsync();
        }
    }
}