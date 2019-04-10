using System.Threading;
using System.Threading.Tasks;
using nhitomi.Core;

namespace nhitomi.Database
{
    public interface IDatabase
    {
        Task<TagSubscriptionInfo[]> GetAllTagSubscriptionsAsync(CancellationToken cancellationToken = default);

        Task<string[]> GetTagSubscriptionsAsync(ulong userId, CancellationToken cancellationToken = default);

        Task<bool> TryAddTagSubscriptionAsync(ulong userId, string tagName,
            CancellationToken cancellationToken = default);

        Task<bool> TryRemoveTagSubscriptionAsync(ulong userId, string tagName,
            CancellationToken cancellationToken = default);

        Task ClearTagSubscriptionsAsync(ulong userId, CancellationToken cancellationToken = default);

        Task<string[]> GetCollectionsAsync(ulong userId, CancellationToken cancellationToken = default);

        Task<CollectionItemInfo[]> GetCollectionAsync(ulong userId, string collectionName,
            CancellationToken cancellationToken = default);

        Task<bool> TryAddToCollectionAsync(ulong userId, string collectionName, IDoujin doujin,
            CancellationToken cancellationToken = default);

        Task<bool> TryRemoveFromCollectionAsync(ulong userId, string collectionName, CollectionItemInfo item,
            CancellationToken cancellationToken = default);

        Task<bool> TryDeleteCollectionAsync(ulong userId, string collectionName,
            CancellationToken cancellationToken = default);

        Task<bool> TrySetCollectionSortAsync(ulong userId, string collectionName, CollectionSortAttribute attribute,
            CancellationToken cancellationToken = default);
    }
}