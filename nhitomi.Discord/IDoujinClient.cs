using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace nhitomi
{
    public interface IDoujinClient : IDisposable
    {
        string Name { get; }
        string IconUrl { get; }

        Task<IDoujin> GetAsync(string id);
        IAsyncEnumerable<IDoujin> Search(string query);

        Task UpdateAsync();
    }
}
