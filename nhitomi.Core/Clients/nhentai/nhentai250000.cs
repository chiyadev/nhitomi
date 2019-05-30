using System;

namespace nhitomi.Core.Clients.nhentai
{
    public class nhentai250000 : ClientTestCase
    {
        public override string DoujinId => "250000";
        public override Type ClientType => typeof(nhentaiClient);

        public override DoujinInfo KnownValue { get; } = new DoujinInfo
        {
            GalleryUrl = "https://nhentai.net/g/250000/",
            PrettyName =
                "Onna no Karada ni Natta Ore wa Danshikou no Shuugaku Ryokou de, Classmate 30-ninZenin to Yarimashita.",
            OriginalName = "女の体になった俺は男子校の修学旅行で、クラスメイト30人全員とヤリました。",
            UploadTime = new DateTime(2019, 5, 30, 9, 39, 59),
            SourceId = "250000",
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
            Artist = "orikawa",
            PageCount = 31
        };
    }
}
