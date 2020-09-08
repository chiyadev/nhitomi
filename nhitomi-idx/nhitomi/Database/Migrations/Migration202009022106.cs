using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace nhitomi.Database.Migrations
{
    /// <summary>
    /// Adds availability field to books.
    /// </summary>
    public class Migration202009022106 : MigrationBase
    {
        public Migration202009022106(IServiceProvider services, ILogger<Migration202009022106> logger) : base(services, logger) { }

        // code deleted on 2020/09/08 to save size
        public override Task RunAsync(CancellationToken cancellationToken = default) => Task.FromException(new Exception("Migration removed."));
    }
}