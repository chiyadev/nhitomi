using System.Threading;
using System.Threading.Tasks;
using nhitomi.Database;
using nhitomi.Models;
using OneOf;
using OneOf.Types;

namespace nhitomi.Controllers
{
    public class VoteServiceOptions { }

    public interface IVoteService
    {
        /// <summary>
        /// Retrieves a vote by a specific user on an object.
        /// </summary>
        Task<DbVote> GetAsync(string userId, nhitomiObject obj, CancellationToken cancellationToken = default);

        /// <summary>
        /// Casts a vote by a specific user on an object.
        /// </summary>
        Task<DbVote> SetAsync(string userId, nhitomiObject obj, VoteType type, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a vote by a specific user on an object.
        /// </summary>
        Task<OneOf<Success, NotFound>> UnsetAsync(string userId, nhitomiObject obj, CancellationToken cancellationToken = default);
    }

    public class VoteService : IVoteService
    {
        readonly IElasticClient _client;

        public VoteService(IElasticClient client)
        {
            _client = client;
        }

        public async Task<DbVote> GetAsync(string userId, nhitomiObject obj, CancellationToken cancellationToken = default)
        {
            var vote = await _client.GetAsync<DbVote>(DbVote.MakeId(userId, obj.Id), cancellationToken);

            return vote?.Target == obj.Type ? vote : null;
        }

        public async Task<DbVote> SetAsync(string userId, nhitomiObject obj, VoteType type, CancellationToken cancellationToken = default)
        {
            var entry = _client.Entry(new DbVote
            {
                Type     = type,
                UserId   = userId,
                Target   = obj.Type,
                TargetId = obj.Id
            });

            return await entry.UpdateAsync(cancellationToken); // this will upsert
        }

        public async Task<OneOf<Success, NotFound>> UnsetAsync(string userId, nhitomiObject obj, CancellationToken cancellationToken = default)
        {
            var entry = await _client.GetEntryAsync<DbVote>(DbVote.MakeId(userId, obj.Id), cancellationToken);

            do
            {
                if (entry.Value?.Target != obj.Type)
                    return new NotFound();
            }
            while (await entry.TryDeleteAsync(cancellationToken));

            return new Success();
        }
    }
}