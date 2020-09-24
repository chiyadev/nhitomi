using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Microsoft.IO;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using nhitomi.Controllers;
using nhitomi.Controllers.OAuth;
using nhitomi.Database;
using nhitomi.Database.Migrations;
using nhitomi.Documentation;
using nhitomi.Models;
using nhitomi.Scrapers;
using nhitomi.Storage;
using Prometheus;
using Swashbuckle.AspNetCore.ReDoc;

namespace nhitomi
{
    public class Startup
    {
        /// <summary>
        /// Naming strategy of model properties.
        /// </summary>
        public static readonly NamingStrategy ModelNamingStrategy = new CamelCaseNamingStrategy
        {
            ProcessDictionaryKeys  = true,
            OverrideSpecifiedNames = true
        };

        /// <summary>
        /// Global singleton <see cref="RecyclableMemoryStreamManager"/>.
        /// </summary>
        public static readonly RecyclableMemoryStreamManager MemoryStreamManager = new RecyclableMemoryStreamManager(
            RecyclableMemoryStreamManager.DefaultBlockSize,
            RecyclableMemoryStreamManager.DefaultLargeBufferMultiple,
            RecyclableMemoryStreamManager.DefaultLargeBufferMultiple * 16,
            true);

        static Startup()
        {
            MemoryStreamManager.AggressiveBufferReturn    = true;
            MemoryStreamManager.MaximumFreeSmallPoolBytes = RecyclableMemoryStreamManager.DefaultBlockSize * 32;          // 4 MiB
            MemoryStreamManager.MaximumFreeLargePoolBytes = RecyclableMemoryStreamManager.DefaultLargeBufferMultiple * 4; // 4 MiB
        }

        readonly IConfigurationRoot _configuration;
        readonly IWebHostEnvironment _environment;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = (IConfigurationRoot) configuration;
            _environment   = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // configuration
            services.AddSingleton(_configuration) // root
                    .AddSingleton<IDynamicOptions, DynamicOptions>()
                    .AddHostedService<ConfigurationReloader>();

            // kestrel
            services.Configure<ServerOptions>(_configuration.GetSection("Server"))
                    .Configure<KestrelServerOptions>(o =>
                     {
                         var server = o.ApplicationServices.GetService<IOptionsMonitor<ServerOptions>>().CurrentValue;

                         if (server.HttpPortDev != null)
                             o.ListenLocalhost(server.HttpPortDev.Value);

                         if (server.HttpPort != null)
                             o.ListenAnyIP(server.HttpPort.Value);

                         if (server.HttpsPort != null && server.CertificatePath != null)
                             o.ListenAnyIP(server.HttpsPort.Value, l => l.UseHttps(server.CertificatePath, server.CertificatePassword));

                         o.Limits.MaxRequestBufferSize       = 1024 * 64;  // 16 KiB
                         o.Limits.MaxRequestLineSize         = 1024 * 8;   // 8 KiB
                         o.Limits.MaxRequestHeadersTotalSize = 1024 * 8;   // 8 KiB
                         o.Limits.MaxRequestBodySize         = 1024 * 256; // 16 KiB
                     })
                    .AddResponseCompression(o =>
                     {
                         o.Providers.Add<GzipCompressionProvider>();
                         o.Providers.Add<BrotliCompressionProvider>();
                     })
                    .AddResponseCaching(o =>
                     {
                         o.UseCaseSensitivePaths = false;

                         // this is for static files
                         o.SizeLimit       = long.MaxValue;
                         o.MaximumBodySize = long.MaxValue;
                     });

            // mvc
            services.AddMvcCore(m =>
                     {
                         m.Filters.Add<PrimitiveResponseWrapperFilter>();
                         m.Filters.Add<RequestValidateQueryFilter>();

                         m.OutputFormatters.RemoveType<StringOutputFormatter>();

                         // model sanitizing binder
                         var modelBinder = new ModelSanitizerModelBinderProvider(m.ModelBinderProviders);

                         m.ModelBinderProviders.Clear();
                         m.ModelBinderProviders.Add(modelBinder);
                     })
                    .AddNewtonsoftJson(o =>
                     {
                         o.SerializerSettings.ContractResolver = new DefaultContractResolver
                         {
                             NamingStrategy = ModelNamingStrategy
                         };

                         o.SerializerSettings.Converters.Add(new StringEnumConverter
                         {
                             NamingStrategy     = ModelNamingStrategy,
                             AllowIntegerValues = true
                         });
                     })
                    .AddApiExplorer()
                    .AddAuthorization()
                    .AddFormatterMappings()
                    .AddDataAnnotations()
                    .AddCors()
                    .AddTestControllers()
                    .AddControllersAsServices();

            services.Configure<ApiBehaviorOptions>(o =>
            {
                o.SuppressMapClientErrors = true;
                o.InvalidModelStateResponseFactory = c =>
                {
                    static string fixFieldCasing(string str)
                        => string.Join('.', str.Split('.').Select(s => ModelNamingStrategy.GetPropertyName(s, false)));

                    var problems = c.ModelState
                                    .Where(x => !x.Value.IsContainerNode && x.Value.Errors.Count != 0)
                                    .Select(x =>
                                     {
                                         var (field, entry) = x;

                                         return new ValidationProblem
                                         {
                                             Field    = ModelNamingStrategy.GetPropertyName(fixFieldCasing(field), false),
                                             Messages = entry.Errors.ToArray(e => e.ErrorMessage ?? e.Exception.ToStringWithTrace(null, _environment.IsProduction()))
                                         };
                                     })
                                    .ToArray();

                    return ResultUtilities.UnprocessableEntity(problems);
                };
            });

            // metrics
            if (_environment.IsProduction())
            {
                services.AddSingleton<IMetricsService, MetricsService>()
                        .AddTransient<IHostedService>(s => s.GetService<IMetricsService>());
            }

            // authentication
            services.AddSingleton<IAuthService, AuthService>()
                    .AddAuthentication(AuthHandler.SchemeName)
                    .AddScheme<AuthOptions, AuthHandler>(AuthHandler.SchemeName, null);

            services.Configure<DiscordOAuthOptions>(_configuration.GetSection("Discord"))
                    .AddSingleton<IDiscordOAuthHandler, DiscordOAuthHandler>();

            // swagger docs
            services.AddSwaggerGen(s =>
            {
                s.SwaggerDoc("docs", new OpenApiInfo
                {
                    Title   = "☆.｡.:*　nhitomi API　.｡.:*☆",
                    Version = VersionInfo.Commit,
                    License = new OpenApiLicense
                    {
                        Name = "MIT License",
                        Url  = new Uri("https://github.com/chiyadev/nhitomi/blob/master/LICENSE")
                    },
                    Description = $"Commit: [{VersionInfo.Commit}](https://github.com/chiyadev/nhitomi/commit/{VersionInfo.Commit})".Trim()
                });

                // https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/1171#issuecomment-501342088
                s.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name         = "Authorization",
                    Type         = SecuritySchemeType.Http,
                    Scheme       = "bearer",
                    BearerFormat = "{header}.{payload}",
                    Description  = "Token authorization using the Bearer scheme."
                });

                s.OperationFilter<AuthenticationOperationFilter>();
                s.OperationFilter<FileDownloadOperationFilter>();
                s.OperationFilter<FileUploadOperationFilter>();
                s.OperationFilter<JsonRequestContentTypeOperationFilter>();
                s.OperationFilter<RequireHumanOperationFilter>();
                s.OperationFilter<RequireReasonOperationFilter>();
                s.OperationFilter<RequireUserOperationFilter>();
                s.OperationFilter<ValidateQueryOperationFilter>();

                s.SchemaFilter<GenericTypeParameterAdditionalSchemaFilter>();
                s.SchemaFilter<NullableRemovingSchemaFilter>();

                s.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{nameof(nhitomi)}.xml"));
            });

            services.AddSwaggerGenNewtonsoftSupport();

            // storage
            services.Configure<StorageOptions>(_configuration.GetSection("Storage"))
                    .AddSingleton<IStorage, RootStorage>();

            // database
            services.Configure<ElasticOptions>(_configuration.GetSection("Elastic"))
                    .Configure<UserServiceOptions>(_configuration.GetSection("User"))
                    .Configure<BookServiceOptions>(_configuration.GetSection("Book"))
                    .Configure<SnapshotServiceOptions>(_configuration.GetSection("Snapshot"))
                    .Configure<VoteServiceOptions>(_configuration.GetSection("Vote"))
                    .Configure<DownloadServiceOptions>(_configuration.GetSection("Download"));

            services.AddSingleton<IElasticClient, ElasticClient>()
                    .AddSingleton<IUserService, UserService>()
                    .AddSingleton<IBookService, BookService>()
                    .AddSingleton<ISnapshotService, SnapshotService>()
                    .AddSingleton<IVoteService, VoteService>()
                    .AddSingleton<ICollectionService, CollectionService>()
                    .AddSingleton<IDownloadService, DownloadService>();

            services.AddSingleton<IWriteControl, WriteControl>()
                    .AddSingleton<IMigrationManager, MigrationManager>();

            // redis
            services.Configure<RedisOptions>(_configuration.GetSection("Redis"))
                    .AddSingleton<IRedisClient, RedisClient>()
                    .AddSingleton<IResourceLocker, RedisResourceLocker>();

            // scrapers
            services.AddSingleton<IScraperService, ScraperService>()
                    .AddSingleton<IBookIndexer, BookIndexer>();

            services.Configure<nhentaiScraperOptions>(_configuration.GetSection("Scraper:nhentai"))
                    .Configure<HitomiScraperOptions>(_configuration.GetSection("Scraper:Hitomi"));

            services.AddInjectableHostedService<IScraper, nhitomiDummyBookScraper>()
                    .AddInjectableHostedService<IScraper, nhentaiScraper>()
                    .AddInjectableHostedService<IScraper, HitomiScraper>();

            // stripe
            services.Configure<StripeServiceOptions>(_configuration.GetSection("Stripe"))
                    .AddSingleton<IStripeService, StripeService>();

            // miscellaneous
            services.AddSingleton<StartupInitializer>()
                    .AddSingleton<ILinkGenerator, LinkGenerator>()
                    .AddSingleton(MemoryStreamManager);

            services.Configure<HttpMessageHandlerOptions>(_configuration.GetSection("HttpHandler"))
                    .AddHttpClient()
                    .AddTransient<HttpMessageHandlerBuilder, HttpProxiedMessageHandlerBuilder>();

            services.Configure<RecaptchaOptions>(_configuration.GetSection("Recaptcha"))
                    .AddSingleton<IRecaptchaValidator, RecaptchaValidator>();
        }

        /// <summary>
        /// Base path for all backend API requests.
        /// </summary>
        public const string ApiBasePath = "/api/v1";

        public void Configure(IApplicationBuilder app)
        {
            var serverOptions = app.ApplicationServices.GetService<IOptionsMonitor<ServerOptions>>().CurrentValue;

            if (serverOptions.ResponseCompression)
                app.UseResponseCompression();

            // backend
            app.Map(ApiBasePath, ConfigureBackend);

            // frontend
            ConfigureFrontend(app);
        }

        void ConfigureFrontend(IApplicationBuilder app)
        {
            // route rewrite
            app.Use((c, n) =>
            {
                switch (c.Request.Method)
                {
                    case "HEAD":
                    case "GET":
                        // frontend is an SPA; if route doesn't exist, rewrite to return the default file
                        if (!_environment.WebRootFileProvider.GetFileInfo(c.Request.Path.Value).Exists)
                            c.Request.Path = "/index.html";

                        return n();

                    default:
                        return ResultUtilities.Status(HttpStatusCode.MethodNotAllowed).ExecuteResultAsync(c);
                }
            });

            // caching
            app.UseResponseCaching();

            // static files
            app.UseStaticFiles(new StaticFileOptions
            {
                HttpsCompression = HttpsCompressionMode.Compress
            });

            if (_environment.IsDevelopment())
                app.Use((c, n) => c.Response.WriteAsync(
                    "404 Not Found; It seems like nhitomi-web was not compiled into nhitomi-idx's static asset directory. This is not necessary during development. " +
                    "However, you should be running nhitomi-web separately using `yarn start`. It is configured to proxy API requests automatically to nhitomi-idx."));
        }

        void ConfigureBackend(IApplicationBuilder app)
        {
            // cors
            app.UseCors(o => o.AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowAnyOrigin());

            // authentication
            app.UseAuthentication();

            // exception handling (must appear before routing)
            app.UseExceptionHandler("/error") // base path is not prepended here because we are using router
               .UseStatusCodePagesWithReExecute("/error/{0}");

            // routing
            app.UseRouting();

            // http metrics
            app.UseHttpMetrics();

            // swagger docs
            app.UseSwagger(s =>
            {
                var servers = new List<OpenApiServer>
                {
                    new OpenApiServer
                    {
                        Url = app.ApplicationServices.GetService<ILinkGenerator>().GetApiLink("/")
                    }
                };

                s.RouteTemplate = "{documentName}.json";
                s.PreSerializeFilters.Add((document, request) => document.Servers = servers);
            });

            app.UseReDoc(r =>
            {
                r.RoutePrefix   = "";
                r.SpecUrl       = "docs.json";
                r.ConfigObject  = new ConfigObject { RequiredPropsFirst = true };
                r.DocumentTitle = "nhitomi API Documentation";
            });

            // authorization
            app.UseAuthorization();

            // controllers
            app.UseEndpoints(e => e.MapControllers());
        }
    }
}