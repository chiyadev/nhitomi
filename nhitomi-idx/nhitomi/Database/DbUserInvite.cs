using System;
using MessagePack;
using Nest;
using nhitomi.Models;

namespace nhitomi.Database
{
    /// <summary>
    /// Represents an invitation created by a user which can be used to register on Nanoka.
    /// </summary>
    [MessagePackObject, ElasticsearchType(RelationName = nameof(UserInvite))]
    public class DbUserInvite : DbObjectBase<UserInvite>, IDbModelConvertible<DbUserInvite, UserInvite>, IHasCreatedTime
    {
        [Key("ac"), Keyword(Name = "ac")]
        public bool Accepted { get; set; }

        [Key("cid"), Keyword(Name = "cid")]
        public string InviterId { get; set; }

        [Key("aid"), Keyword(Name = "aid")]
        public string InviteeId { get; set; }

        [Key("tc"), Date(Name = "tc")]
        public DateTime CreatedTime { get; set; }

        [Key("te"), Date(Name = "te")]
        public DateTime ExpiryTime { get; set; }

        [Key("ta"), Date(Name = "ta")]
        public DateTime? AcceptedTime { get; set; }

        public override void MapTo(UserInvite model)
        {
            base.MapTo(model);

            model.Accepted     = Accepted;
            model.InviterId    = InviterId;
            model.InviteeId    = InviteeId;
            model.CreatedTime  = CreatedTime;
            model.ExpiryTime   = ExpiryTime;
            model.AcceptedTime = AcceptedTime;
        }

        public override void MapFrom(UserInvite model)
        {
            base.MapFrom(model);

            Accepted     = model.Accepted;
            InviterId    = model.InviterId;
            InviteeId    = model.InviteeId;
            CreatedTime  = model.CreatedTime;
            ExpiryTime   = model.ExpiryTime;
            AcceptedTime = model.AcceptedTime;
        }
    }
}