using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace nhitomi.Database.Migrations
{
    /// <summary>
    /// Initial migration that does not do anything.
    /// </summary>
    public class Migration202001010000 : MigrationBase
    {
        public Migration202001010000(IOptionsMonitor<ElasticOptions> options, Nest.ElasticClient client, ILogger<Migration202001010000> logger) : base(options, client, logger) { }

        public override Task RunAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}