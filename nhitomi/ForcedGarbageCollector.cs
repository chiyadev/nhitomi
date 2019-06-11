using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace nhitomi
{
    public class ForcedGarbageCollector : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

                //todo: this is very very very very very very bad
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, true, true);
            }
        }
    }
}
