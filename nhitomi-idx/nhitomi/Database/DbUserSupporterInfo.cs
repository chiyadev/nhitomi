using System;
using MessagePack;
using Nest;
using nhitomi.Models;

namespace nhitomi.Database
{
    [MessagePackObject]
    public class DbUserSupporterInfo : DbModelBase<UserSupporterInfo>, IDbModelConvertible<DbUserSupporterInfo, UserSupporterInfo>
    {
        [Key("Ts"), Date(Name = "Ts")]
        public DateTime? StartTime { get; set; }

        [Key("Te"), Date(Name = "Te")]
        public DateTime? EndTime { get; set; }

        [Key("da"), Number(Name = "da")]
        public double TotalDays { get; set; }

        [Key("sp"), Number(Name = "sp")]
        public double TotalSpending { get; set; }

        public override void MapTo(UserSupporterInfo model, IServiceProvider services)
        {
            base.MapTo(model, services);

            model.StartTime     = StartTime;
            model.EndTime       = EndTime;
            model.TotalDays     = TotalDays;
            model.TotalSpending = TotalSpending;
        }

        public override void MapFrom(UserSupporterInfo model, IServiceProvider services)
        {
            base.MapFrom(model, services);

            StartTime     = model.StartTime;
            EndTime       = model.EndTime;
            TotalDays     = model.TotalDays;
            TotalSpending = model.TotalSpending;
        }
    }
}