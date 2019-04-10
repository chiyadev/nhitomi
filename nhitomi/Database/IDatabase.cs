using System.Threading;
using System.Threading.Tasks;

namespace nhitomi.Database
{
    public interface IDatabase
    {
        Task<TagSubscriptionInfo[]> GetAllTagSubscriptionsAsync(CancellationToken cancellationToken = default);

        Task<string[]> GetTagSubscriptionsAsync(ulong userId, CancellationToken cancellationToken = default);

        Task AddTagSubscriptionAsync(ulong userId, string tagName, CancellationToken cancellationToken = default);

        Task RemoveTagSubscriptionAsync(ulong userId, string tagName, CancellationToken cancellationToken = default);

        Task<string[]> GetCollectionsAsync(ulong userId, CancellationToken cancellationToken = default);

        Task<DoujinSummary[]> GetCollectionAsync(ulong userId, string collectionName,
            CancellationToken cancellationToken = default);

        Task AddToCollectionAsync(ulong userId, string collectionName, DoujinSummary summary,
            CancellationToken cancellationToken = default);

        Task RemoveFromCollectionAsync(ulong userId, string collectionName, DoujinSummary summary,
            CancellationToken cancellationToken = default);
    }
}