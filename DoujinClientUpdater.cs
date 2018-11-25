using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace nhitomi
{
    public class DoujinClientUpdater : IBackgroundService
    {
        readonly AppSettings _settings;
        readonly ISet<IDoujinClient> _clients;

        public DoujinClientUpdater(
            IOptions<AppSettings> options,
            ISet<IDoujinClient> clients
        )
        {
            _settings = options.Value;
            _clients = clients;
        }

        public async Task RunAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // Update clients
                await Task.WhenAll(_clients.Select(c => c.UpdateAsync()));

                // Sleep
                await Task.Delay(
                    TimeSpan.FromMinutes(_settings.Doujin.UpdateInterval),
                    token
                );
            }
        }
    }
}