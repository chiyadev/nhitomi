using System.Threading;
using System.Threading.Tasks;
using nhitomi.Database;

namespace nhitomi
{
    public interface IDatabase
    {
        Task<TagSubscriptionInfo[]> GetTagSubscriptionsAsync(CancellationToken cancellationToken = default);
    }
}
