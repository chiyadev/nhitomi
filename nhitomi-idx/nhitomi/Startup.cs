using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using nhitomi.Discord;

namespace nhitomi
{
    public class Startup
    {
        readonly IConfiguration _configuration;
        readonly IWebHostEnvironment _environment;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment   = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // configuration
            services.Configure<DiscordOptions>(_configuration.GetSection("Discord"))
                    .Configure<InteractiveOptions>(_configuration.GetSection("Interactive"));

            // discord
            services.AddSingleton<IDiscordClient, DiscordClient>()
                    .AddSingleton<IDiscordMessageHandler, DiscordMessageHandler>()
                    .AddSingleton<IDiscordReactionHandler, DiscordReactionHandler>()
                    .AddSingleton<IDiscordLocaleProvider, DiscordLocaleProvider>()
                    .AddSingleton<IUserFilter, DefaultUserFilter>()
                    .AddHostedService<DiscordConnectionManager>();

            services.AddSingleton<IInteractiveManager, InteractiveManager>()
                    .AddSingleton<IReplyRenderer, ReplyRenderer>();

            // mvc
            services.AddMvcCore()
                    .AddApiExplorer()
                    .AddAuthorization()
                    .AddFormatterMappings()
                    .AddDataAnnotations()
                    .AddCors()
                    .AddControllersAsServices();

            // other
            services.AddTransient<MemoryInfo>();
        }

        public void Configure(IApplicationBuilder app)
        {
            // cors
            app.UseCors(o => o.AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowAnyOrigin());

            // authentication
            app.UseAuthentication();

            // exception handling
            app.UseExceptionHandler("/error")
               .UseStatusCodePagesWithReExecute("/error/{0}");

            // routing
            app.UseRouting();

            // authorization
            app.UseAuthorization();

            // controllers
            app.UseEndpoints(x => x.MapControllers());
        }
    }
}