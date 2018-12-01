using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace nhitomi
{
    public interface IDoujinClient : IDisposable
    {
        string Name { get; }
        string Url { get; }
        string IconUrl { get; }

        Regex GalleryRegex { get; }

        Task<IDoujin> GetAsync(string id);
        Task<IAsyncEnumerable<IDoujin>> SearchAsync(string query);

        Task UpdateAsync();
    }
}
