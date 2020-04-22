using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using nhitomi.Controllers;
using nhitomi.Database;
using nhitomi.Discord;
using nhitomi.Documentation;
using nhitomi.Models;
using nhitomi.Storage;
using Swashbuckle.AspNetCore.ReDoc;

namespace nhitomi
{
    public class Startup
    {
        readonly IConfigurationRoot _configuration;
        readonly IWebHostEnvironment _environment;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = (IConfigurationRoot) configuration;
            _environment   = environment;
        }

        /// <summary>
        /// Naming strategy of model properties.
        /// </summary>
        public static readonly NamingStrategy ModelNamingStrategy = new CamelCaseNamingStrategy
        {
            ProcessDictionaryKeys  = true,
            OverrideSpecifiedNames = true
        };

        public void ConfigureServices(IServiceCollection services)
        {
            // configuration
            services.AddSingleton(_configuration)
                    .Configure<ServerOptions>(_configuration.GetSection(nameof(CompositeConfig.Server)))
                    .Configure<StorageOptions>(_configuration.GetSection(nameof(CompositeConfig.Storage)))
                    .Configure<ElasticOptions>(_configuration.GetSection(nameof(CompositeConfig.Elastic)))
                    .Configure<RedisOptions>(_configuration.GetSection(nameof(CompositeConfig.Redis)))
                    .Configure<RecaptchaOptions>(_configuration.GetSection(nameof(CompositeConfig.Recaptcha)))
                    .Configure<UserServiceOptions>(_configuration.GetSection(nameof(CompositeConfig.User)))
                    .Configure<BookServiceOptions>(_configuration.GetSection(nameof(CompositeConfig.Book)))
                    .Configure<SnapshotServiceOptions>(_configuration.GetSection(nameof(CompositeConfig.Snapshot)))
                    .Configure<DiscordOptions>(_configuration.GetSection(nameof(CompositeConfig.Discord)));

            services.AddHostedService<ConfigurationReloader>();

            // kestrel
            services.Configure<KestrelServerOptions>(o =>
            {
                var server = _configuration.GetSection(nameof(CompositeConfig.Server)).Get<ServerOptions>();

                if (_environment.IsDevelopment())
                {
                    o.ListenLocalhost(server.HttpPortDev);
                }
                else
                {
                    o.ListenAnyIP(server.HttpPort);

                    if (server.CertificatePath != null)
                        o.ListenAnyIP(server.HttpsPort, l => l.UseHttps(server.CertificatePath, server.CertificatePassword));
                }

                o.Limits.MaxRequestBufferSize       = 1024 * 64;  // 16 KiB
                o.Limits.MaxRequestLineSize         = 1024 * 8;   // 8 KiB
                o.Limits.MaxRequestHeadersTotalSize = 1024 * 8;   // 8 KiB
                o.Limits.MaxRequestBodySize         = 1024 * 256; // 16 KiB
            });

            services.AddResponseCompression(o =>
            {
                o.Providers.Add<GzipCompressionProvider>();
                o.Providers.Add<BrotliCompressionProvider>();
            });

            services.AddResponseCaching(o =>
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

            services.AddSingleton<IAuthService, AuthService>()
                    .AddAuthentication(AuthHandler.SchemeName)
                    .AddScheme<AuthOptions, AuthHandler>(AuthHandler.SchemeName, null);

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

            // swagger docs
            services.AddSwaggerGen(s =>
            {
                s.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title       = "nhitomi API",
                    Version     = VersionInfo.Version.ToString(),
                    Description = "nhitomi HTTP API Documentation"
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

                s.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "nhitomi.xml"));
            });

            services.AddSwaggerGenNewtonsoftSupport();

            // storage
            services.AddSingleton<IStorage, DefaultStorage>();

            // database
            services.AddSingleton<IElasticClient, ElasticClient>()
                    .AddSingleton<IUserService, UserService>()
                    .AddSingleton<IBookService, BookService>()
                    .AddSingleton<ISnapshotService, SnapshotService>();

            services.AddSingleton<IRedisClient, RedisClient>()
                    .AddSingleton<ICacheManager, RedisCacheManager>()
                    .AddSingleton<IResourceLocker, RedisResourceLocker>();

            // discord
            services.AddSingleton<IDiscordClient, DiscordClient>()
                    .AddSingleton<IDiscordMessageHandler, DiscordMessageHandler>()
                    .AddSingleton<IDiscordReactionHandler, DiscordReactionHandler>()
                    .AddSingleton<IDiscordLocaleProvider, DiscordLocaleProvider>()
                    .AddSingleton<IUserFilter, DefaultUserFilter>()
                    .AddHostedService<DiscordConnectionManager>();

            services.AddSingleton<IInteractiveManager, InteractiveManager>()
                    .AddSingleton<IReplyRenderer, ReplyRenderer>();

            // other
            services.AddHttpClient()
                    .AddHttpContextAccessor()
                    .AddSingleton<IRecaptchaValidator, RecaptchaValidator>()
                    .AddSingleton<IImageProcessor, SkiaImageProcessor>()
                    .AddTransient<MemoryInfo>()
                    .AddSingleton<StartupInitializer>();
        }

        /// <summary>
        /// Base path for all backend API requests.
        /// </summary>
        public const string ApiBasePath = "/api/v1";

        public void Configure(IApplicationBuilder app)
        {
            var server = _configuration.GetSection(nameof(CompositeConfig.Server)).Get<ServerOptions>();

            if (server.ResponseCompression)
                app.UseResponseCompression();

            // backend
            app.Map(ApiBasePath, ConfigureBackend);

            // frontend
            ConfigureFrontend(app);
        }

        void ConfigureFrontend(IApplicationBuilder app)
        {
            var server = _configuration.GetSection(nameof(CompositeConfig.Server)).Get<ServerOptions>();

            // route rewrite
            app.Use((c, n) =>
            {
                switch (c.Request.Method)
                {
                    case "HEAD":
                    case "GET":
                        if (c.Request.Path.Value == "/")
                            c.Request.Path = server.DefaultFile;

                        // frontend is an SPA; if route doesn't exist, rewrite to return the default file
                        else if (!_environment.WebRootFileProvider.GetFileInfo(c.Request.Path.Value).Exists)
                            c.Request.Path = server.DefaultFile;

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
        }

        void ConfigureBackend(IApplicationBuilder app)
        {
            // cors
            app.UseCors(o => o.AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowAnyOrigin());

            // authentication
            app.UseAuthentication();

            // exception handling
            app.UseExceptionHandler("/error") // base path is not prepended here because we are using router
               .UseStatusCodePagesWithReExecute("/error/{0}");

            // swagger docs
            app.UseSwagger(s =>
            {
                s.RouteTemplate = "/docs/{documentName}.json";

                s.PreSerializeFilters.Add((document, request) =>
                {
                    var scheme = request.Scheme;

                    if (request.Host.Host != "localhost")
                        scheme = "https"; // hack for cloudflare

                    document.Servers = new List<OpenApiServer>
                    {
                        new OpenApiServer
                        {
                            Url = $"{scheme}://{request.Host}{ApiBasePath}"
                        }
                    };
                });
            });

            app.UseReDoc(r =>
            {
                r.RoutePrefix   = "docs";
                r.DocumentTitle = "nhitomi API";

                r.SpecUrl      = "v1.json";
                r.ConfigObject = new ConfigObject { RequiredPropsFirst = true };
            });

            // routing
            app.UseRouting();

            // authorization
            app.UseAuthorization();

            // controllers
            app.UseEndpoints(e => e.MapControllers());
        }
    }
}