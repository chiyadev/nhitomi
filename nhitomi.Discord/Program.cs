// Copyright (c) 2018 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

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
        static async Task Main(string[] args)
        {
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
                    .Select(s => s.RunAsync(token));

                // Run background services
                await Task.WhenAll(tasks);
            }
            finally
            {
                // Stop session
                await _discord.StopSessionAsync();
            }
        }
    }
}