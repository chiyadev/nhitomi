using System;
using MessagePack;
using Nest;
using nhitomi.Models;

namespace nhitomi.Database
{
    [MessagePackObject, ElasticsearchType(RelationName = nameof(DonationInfo))]
    public class DbDonationInfo : DbObjectBase<DonationInfo>, IDbModelConvertible<DbDonationInfo, DonationInfo, DonationInfoBase>
    {
        [Key("pr"), Number(Name = "pr")]
        public double Progress { get; set; }

        public override void MapTo(DonationInfo model, IServiceProvider services)
        {
            base.MapTo(model, services);

            model.Progress = Progress;
        }

        public override void MapFrom(DonationInfo model, IServiceProvider services)
        {
            base.MapFrom(model, services);

            Progress = model.Progress;
        }
    }
}