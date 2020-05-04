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
    public class DbUser : DbObjectBase<User>, IDbHasType, IDbModelConvertible<DbUser, User, UserBase>, IHasUpdatedTime
    {
        [IgnoreMember, Ignore]
        ObjectType IDbHasType.Type => ObjectType.User;

        [Key("Tc"), Date(Name = "Tc")]
        public DateTime CreatedTime { get; set; }

        [Key("Tu"), Date(Name = "Tu")]
        public DateTime UpdatedTime { get; set; }

        [Key("un"), Keyword(Name = "un")]
        public string Username { get; set; }

        [Key("em"), Keyword(Name = "em")]
        public string Email { get; set; }

        [Key("re"), Object(Name = "re", Enabled = false)]
        public DbUserRestriction[] Restrictions { get; set; }

        [Key("pe"), Keyword(Name = "pe", DocValues = false)]
        public UserPermissions[] Permissions { get; set; }

        [Key("ln"), Keyword(Name = "ln", DocValues = false)]
        public LanguageType Language { get; set; } = LanguageType.English;

        [Key("cd"), Object(Name = "cd", Enabled = false)]
        public DbUserDiscordConnection DiscordConnection { get; set; }

        [Key("cA"), Keyword(Name = "cA", Index = false)]
        public bool AllowSharedCollections { get; set; } = true;

        [Key("cb"), Keyword(Name = "cd", Index = false)]
        public string DefaultBookCollection { get; set; }

        /// <summary>
        /// Returns true if this user has the specified permissions.
        /// This method allows <see cref="UserPermissions.Administrator"/> bypass.
        /// </summary>
        public bool HasPermissions(UserPermissions permissions)
            => Permissions.ToBitwise().Compose(p => p.HasFlag(UserPermissions.Administrator) || p.HasFlag(permissions));

        public override void MapTo(User model)
        {
            base.MapTo(model);

            model.CreatedTime            = CreatedTime;
            model.UpdatedTime            = UpdatedTime;
            model.Username               = Username;
            model.Email                  = Email;
            model.Restrictions           = Restrictions?.ToArray(r => r.Convert()) ?? Array.Empty<UserRestriction>();
            model.Permissions            = Permissions;
            model.Language               = Language;
            model.DiscordConnection      = DiscordConnection?.Convert();
            model.AllowSharedCollections = AllowSharedCollections;
            model.DefaultBookCollection  = DefaultBookCollection;
        }

        public override void MapFrom(User model)
        {
            base.MapFrom(model);

            CreatedTime            = model.CreatedTime;
            UpdatedTime            = model.UpdatedTime;
            Username               = model.Username;
            Email                  = model.Email;
            Restrictions           = model.Restrictions?.ToArray(r => new DbUserRestriction().Apply(r));
            Permissions            = model.Permissions.ToDistinctFlags();
            Language               = model.Language;
            DiscordConnection      = model.DiscordConnection == null ? null : new DbUserDiscordConnection().Apply(model.DiscordConnection);
            AllowSharedCollections = model.AllowSharedCollections;
            DefaultBookCollection  = model.DefaultBookCollection;
        }

#region Cached

        /// <summary>
        /// This is a cached property for querying.
        /// </summary>
        [IgnoreMember, Keyword(Name = "cdi", DocValues = false), DbCached]
        public ulong? DiscordId { get; set; }

        public override void UpdateCache()
        {
            base.UpdateCache();

            DiscordId = DiscordConnection?.Id;
        }

#endregion

        public static implicit operator nhitomiObject(DbUser user) => new nhitomiObject(ObjectType.User, user.Id);
    }
}