using System;
using MessagePack;
using Nest;
using nhitomi.Models;

namespace nhitomi.Database
{
    /// <summary>
    /// Represents a vote casted by a user on an object.
    /// </summary>
    [MessagePackObject, ElasticsearchType(RelationName = nameof(Vote))]
    public class DbVote : DbObjectBase<Vote>, IDbModelConvertible<DbVote, Vote>
    {
        [Key("t"), Keyword(Name = "y", DocValues = false)]
        public VoteType Type { get; set; }

        [Key("u"), Keyword(Name = "u", DocValues = false)]
        public string UserId
        {
            get
            {
                ParseId(Id, out var userId, out _);
                return userId;
            }
            set => Id = MakeId(value, TargetId);
        }

        [Key("x"), Keyword(Name = "x", DocValues = false)]
        public ObjectType Target { get; set; }

        [Key("z"), Keyword(Name = "e", DocValues = false)]
        public string TargetId
        {
            get
            {
                ParseId(Id, out _, out var targetId);
                return targetId;
            }
            set => Id = MakeId(UserId, value);
        }

        public override void MapTo(Vote model, IServiceProvider services)
        {
            base.MapTo(model, services);

            model.UserId   = UserId;
            model.Target   = Target;
            model.TargetId = TargetId;
            model.Type     = Type;
        }

        public override void MapFrom(Vote model, IServiceProvider services)
        {
            base.MapFrom(model, services);

            UserId   = model.UserId;
            Target   = model.Target;
            TargetId = model.TargetId;
            Type     = model.Type;
        }

        public static string MakeId(string userId, string targetId) => $"{userId}:{targetId}";

        public static void ParseId(string id, out string userId, out string targetId)
        {
            var parts = id?.Split(':', 2);

            userId   = parts?[0];
            targetId = parts?.Length == 2 ? parts[1] : null;
        }
    }
}