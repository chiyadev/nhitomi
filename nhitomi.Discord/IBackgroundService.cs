using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace nhitomi
{
    public interface IBackgroundService
    {
        Task RunAsync(CancellationToken token);
    }
}