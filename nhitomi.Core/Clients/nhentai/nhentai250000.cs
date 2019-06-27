using System;

namespace nhitomi.Core.Clients.nhentai
{
    public class nhentai250000 : ClientTestCase
    {
        public override string DoujinId => "250000";
        public override Type ClientType => typeof(nhentaiClient);

        public override DoujinInfo KnownValue { get; } = new DoujinInfo
        {
            PrettyName =
                "Onna no Karada ni Natta Ore wa Danshikou no Shuugaku Ryokou de, Classmate 30-ninZenin to Yarimashita.",
            OriginalName = "女の体になった俺は男子校の修学旅行で、クラスメイト30人全員とヤリました。",
            UploadTime   = DateTime.Parse("2018-10-15T15:17:55+00:00").ToUniversalTime(),
            SourceId     = "250000",
            Tags = new[]
            {
                "full censorship",
                "body swap",
                "teacher"
            },
            Language = "chinese",
            Categories = new[]
            {
                "manga"
            },
            Artist    = "orikawa",
            PageCount = 31
        };
    }
}