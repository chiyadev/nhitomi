using MessagePack;
using Nest;
using nhitomi.Models;

namespace nhitomi.Database
{
    [MessagePackObject]
    public class DbUserDiscordConnection : DbModelBase<UserDiscordConnection>, IDbModelConvertible<DbUserDiscordConnection, UserDiscordConnection>
    {
        [Key("i"), Keyword(Name = "xi", DocValues = false)]
        public ulong Id { get; set; }

        [Key("u"), Keyword(Name = "u", DocValues = false)]
        public string Username { get; set; }

        [Key("d"), Keyword(Name = "d", DocValues = false)]
        public int Discriminator { get; set; }

        [Key("v"), Keyword(Name = "v", DocValues = false)]
        public bool Verified { get; set; }

        [Key("e"), Keyword(Name = "e", DocValues = false)]
        public string Email { get; set; }

        public override void MapTo(UserDiscordConnection model)
        {
            base.MapTo(model);

            model.Id            = Id;
            model.Username      = Username;
            model.Discriminator = Discriminator;
            model.Verified      = Verified;
            model.Email         = Email;
        }

        public override void MapFrom(UserDiscordConnection model)
        {
            base.MapFrom(model);

            Id            = model.Id;
            Username      = model.Username;
            Discriminator = model.Discriminator;
            Verified      = model.Verified;
            Email         = model.Email;
        }
    }
}