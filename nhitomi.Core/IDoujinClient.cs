using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace nhitomi.Core
{
    public interface IDoujinClient : IDisposable
    {
        string Name { get; }
        string Url { get; }

        Task<DoujinInfo> GetAsync(string id, CancellationToken cancellationToken = default);
        Task<IEnumerable<string>> EnumerateAsync(string startId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets image URL of the pages in the doujin.
        /// </summary>
        IEnumerable<string> PopulatePages(Doujin doujin);
    }
}