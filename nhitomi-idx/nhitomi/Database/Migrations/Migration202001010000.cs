using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace nhitomi.Database.Migrations
{
    /// <summary>
    /// Initial migration that does not do anything.
    /// </summary>
    public class Migration202001010000 : MigrationBase
    {
        public Migration202001010000(IServiceProvider services, ILogger<Migration202001010000> logger) : base(services, logger) { }

        public override Task RunAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}