using System;
using MessagePack;
using Nest;
using nhitomi.Models;

namespace nhitomi.Database
{
    /// <summary>
    /// Represents a nhitomi user.
    /// </summary>
    [MessagePackObject, ElasticsearchType(RelationName = nameof(User))]
    public class DbUser : DbObjectBase<User>, IDbModelConvertible<DbUser, User, UserBase>, IHasUpdatedTime, IDbSupportsSnapshot
    {
        [Key("Tc"), Date(Name = "Tc")]
        public DateTime CreatedTime { get; set; }

        [Key("Tu"), Date(Name = "Tu")]
        public DateTime UpdatedTime { get; set; }

        [Key("un"), Keyword(Name = "un")]
        public string Username { get; set; }

        /// <summary>
        /// Cannot query against this property.
        /// </summary>
        [Key("pw"), Keyword(Name = "pw", Index = false)]
        public string Password { get; set; }

        [Key("em"), Keyword(Name = "em")]
        public string Email { get; set; }

        [Key("re"), Object(Name = "re", Enabled = false)]
        public DbUserRestriction[] Restrictions { get; set; }

        [Key("pe"), Keyword(Name = "pe")]
        public UserPermissions[] Permissions { get; set; }

        /// <summary>
        /// Returns true if this user has the specified permissions.
        /// This method allows <see cref="UserPermissions.Administrator"/> bypass.
        /// </summary>
        public bool HasPermissions(UserPermissions permissions)
            => Permissions.ToBitwise().Compose(p => p.HasFlag(UserPermissions.Administrator) || p.HasFlag(permissions));

        public override void MapTo(User model)
        {
            base.MapTo(model);

            model.CreatedTime  = CreatedTime;
            model.UpdatedTime  = UpdatedTime;
            model.Username     = Username;
            model.Email        = Email;
            model.Restrictions = Restrictions?.ToArray(r => r.Convert()) ?? Array.Empty<UserRestriction>();
            model.Permissions  = Permissions;
        }

        public override void MapFrom(User model)
        {
            base.MapFrom(model);

            CreatedTime  = model.CreatedTime;
            UpdatedTime  = model.UpdatedTime;
            Username     = model.Username;
            Email        = model.Email;
            Restrictions = model.Restrictions?.ToArray(r => new DbUserRestriction().Apply(r));
            Permissions  = model.Permissions.ToDistinctFlags();
        }

        [IgnoreMember, Ignore]
        public SnapshotTarget SnapshotTarget => SnapshotTarget.User;

        public static implicit operator nhitomiObject(DbUser user) => new nhitomiObject(user.SnapshotTarget, user.Id);
    }
}