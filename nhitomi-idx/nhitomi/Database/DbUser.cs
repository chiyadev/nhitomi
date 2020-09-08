using System;
using System.Collections.Generic;
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

        [Key("Cs"), Keyword(Name = "Cs", Index = false)]
        public bool AllowSharedCollections { get; set; } = true;

        [Key("Cp"), Object(Name = "Cp", Enabled = false)]
        public Dictionary<ObjectType, Dictionary<SpecialCollection, string>> SpecialCollections { get; set; }

        [Key("sp"), Object(Name = "sp", Enabled = false)]
        public DbUserSupporterInfo SupporterInfo { get; set; }

        /// <summary>
        /// Returns true if this user has the specified permissions.
        /// This method allows <see cref="UserPermissions.Administrator"/> bypass.
        /// </summary>
        public bool HasPermissions(UserPermissions permissions)
            => Permissions.ToBitwise().Compose(p => p.HasFlag(UserPermissions.Administrator) || p.HasFlag(permissions));

        public override void MapTo(User model, IServiceProvider services)
        {
            base.MapTo(model, services);

            model.CreatedTime            = CreatedTime;
            model.UpdatedTime            = UpdatedTime;
            model.Username               = Username;
            model.Email                  = Email;
            model.Restrictions           = Restrictions?.ToArray(r => r.Convert(services)) ?? Array.Empty<UserRestriction>();
            model.Permissions            = Permissions;
            model.Language               = Language;
            model.DiscordConnection      = DiscordConnection?.Convert(services);
            model.AllowSharedCollections = AllowSharedCollections;
            model.SpecialCollections     = SpecialCollections;
            model.SupporterInfo          = SupporterInfo?.Convert(services);

            if (model.Restrictions != null)
                Array.Sort(model.Restrictions, (a, b) => a.StartTime.CompareTo(b.StartTime));
        }

        public override void MapFrom(User model, IServiceProvider services)
        {
            base.MapFrom(model, services);

            CreatedTime            = model.CreatedTime;
            UpdatedTime            = model.UpdatedTime;
            Username               = model.Username;
            Email                  = model.Email;
            Restrictions           = model.Restrictions?.ToArray(r => new DbUserRestriction().Apply(r, services));
            Permissions            = model.Permissions.ToDistinctFlags();
            Language               = model.Language;
            DiscordConnection      = model.DiscordConnection == null ? null : new DbUserDiscordConnection().Apply(model.DiscordConnection, services);
            AllowSharedCollections = model.AllowSharedCollections;
            SpecialCollections     = model.SpecialCollections;
            SupporterInfo          = model.SupporterInfo == null ? null : new DbUserSupporterInfo().Apply(model.SupporterInfo, services);
        }

#region Cached

        /// <summary>
        /// This is a cached property for querying.
        /// </summary>
        [IgnoreMember, Keyword(Name = "cdi", DocValues = false), DbCached]
        public ulong? DiscordId { get; set; }

        /// <summary>
        /// This is a cached property for querying.
        /// </summary>
        [IgnoreMember, Date(Name = "sTe"), DbCached]
        public DateTime? SupporterEndTime { get; set; }

        /// <summary>
        /// This is a cached property for querying.
        /// </summary>
        [IgnoreMember, Number(Name = "ssp"), DbCached]
        public double SupporterTotalSpending { get; set; }

        public override void UpdateCache(IServiceProvider services)
        {
            base.UpdateCache(services);

            DiscordId              = DiscordConnection?.Id;
            SupporterEndTime       = SupporterInfo?.EndTime;
            SupporterTotalSpending = SupporterInfo?.TotalSpending ?? 0;
        }

#endregion

        public static implicit operator nhitomiObject(DbUser user) => new nhitomiObject(ObjectType.User, user.Id);
    }
}