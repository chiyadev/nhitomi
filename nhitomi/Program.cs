// Copyright (c) 2018-2019 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace nhitomi
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            await WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) => config
                    .SetBasePath(Environment.CurrentDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile("appsecrets.json", optional: true, reloadOnChange: true))
                .ConfigureLogging((context, logging) => logging
                    .AddConfiguration(context.Configuration.GetSection("logging"))
                    .AddConsole()
                    .AddDebug())
                .UseStartup<Startup>()
                .Build()
                .RunAsync();
        }
    }
}