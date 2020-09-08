using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace nhitomi.Database.Migrations
{
    /// <summary>
    /// Adds supporter information field to users.
    /// </summary>
    public class Migration202009082258 : MigrationBase
    {
        public Migration202009082258(IServiceProvider services, ILogger<Migration202009082258> logger) : base(services, logger) { }

        public override async Task RunAsync(CancellationToken cancellationToken = default)
        {
            var (source, destination) = await GetReindexTargets("user", cancellationToken);

            // SupporterInfo field can be null, so simply remap model
            await MapIndexAsync<DbUser, DbUser>(source, destination, u => u, cancellationToken);
        }
    }
}