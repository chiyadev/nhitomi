using System;
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

        [Key("e"), Keyword(Name = "e", DocValues = false)]
        public string Email { get; set; }

        public override void MapTo(UserDiscordConnection model, IServiceProvider services)
        {
            base.MapTo(model, services);

            model.Id            = Id;
            model.Username      = Username;
            model.Discriminator = Discriminator;
            model.Email         = Email;
        }

        public override void MapFrom(UserDiscordConnection model, IServiceProvider services)
        {
            base.MapFrom(model, services);

            Id            = model.Id;
            Username      = model.Username;
            Discriminator = model.Discriminator;
            Email         = model.Email;
        }
    }
}