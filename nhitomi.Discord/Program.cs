using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace nhitomi
{
    public class Program
    {
        static async Task setupAsync()
        {
            await Console.Out.WriteLineAsync($"nhitomi — Discord doujinshi bot");

            if (File.Exists("appsecrets.json"))
                return;

            await Console.Out.WriteAsync($"Bot token: ");

            var token = await Console.In.ReadLineAsync();
            await File.WriteAllTextAsync("appsecrets.json", "{\"discord\":{\"token\":\"" + token + "\"}}");
        }

        static async Task Main(string[] args)
        {
            await setupAsync();

            // Configure services
            var services = new ServiceCollection();
            var startup = new Startup
            {
                Configuration = new ConfigurationBuilder()
                    .SetBasePath(Environment.CurrentDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile("appsecrets.json", optional: true, reloadOnChange: true)
                    .Build()
            };

            startup.ConfigureServices(services);

            // Run asynchronously
            var provider = services.BuildServiceProvider();
            await provider.GetRequiredService<Program>().RunAsync(args, new CancellationTokenSource().Token);
        }

        readonly DiscordService _discord;
        readonly ISet<IBackgroundService> _services;
        readonly ILogger _logger;

        public Program(
            DiscordService discord,
            ISet<IBackgroundService> backgroundServices,
            ILogger<Program> logger
        )
        {
            _discord = discord;
            _services = backgroundServices;
            _logger = logger;
        }

        public async Task RunAsync(string[] args, CancellationToken token)
        {
            // Start session
            await _discord.StartSessionAsync();

            try
            {
                var tasks = _services
                    .Select(s => s.RunAsync(token))
                    .Append(reportInternal(token));

                // Run background services
                await Task.WhenAll(tasks);
            }
            finally
            {
                // Stop session
                await _discord.StopSessionAsync();
            }
        }

        async Task reportInternal(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                using (var process = Process.GetCurrentProcess())
                    _logger.LogInformation($"Process {process.Id}: memory usage {GC.GetTotalMemory(true).GetBytesReadable()} (managed)");

                await Task.Delay(TimeSpan.FromMinutes(10), token);
            }
        }
    }
}