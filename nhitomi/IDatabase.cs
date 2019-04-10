using System.Threading;
using System.Threading.Tasks;
using nhitomi.Database;

namespace nhitomi
{
    public interface IDatabase
    {
        Task<TagSubscriptionInfo[]> GetTagSubscriptionsAsync(CancellationToken cancellationToken = default);

        Task<string[]> GetTagSubscriptionsAsync(ulong userId, CancellationToken cancellationToken = default);

        Task AddTagSubscriptionAsync(ulong userId, string tagName, CancellationToken cancellationToken = default);

        Task RemoveTagSubscriptionAsync(ulong userId, string tagName, CancellationToken cancellationToken = default);
    }
}