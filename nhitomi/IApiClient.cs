using System.Threading;
using System.Threading.Tasks;
using nhitomi.Core;

namespace nhitomi
{
    public interface IApiClient
    {
        Task<Doujin> GetDoujinAsync(string source, string id, CancellationToken cancellationToken = default);
    }
}