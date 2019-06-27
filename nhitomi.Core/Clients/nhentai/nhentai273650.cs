using System;

namespace nhitomi.Core.Clients.nhentai
{
    public class nhentai273650 : ClientTestCase
    {
        public override string DoujinId => "273650";
        public override Type ClientType => typeof(nhentaiClient);

        public override DoujinInfo KnownValue { get; } = new DoujinInfo
        {
            PrettyName   = "Imouto wa Onii-chan to Shouraiteki ni Flag o Tatetai",
            OriginalName = "いもうとはお兄ちゃんと将来的にフラグをたてたい",
            UploadTime   = DateTime.Parse("2019-05-28T06:17:24+00:00").ToUniversalTime(),
            SourceId     = "273650",
            Group        = "astronomy",
            Language     = "japanese",
            Characters = new[]
            {
                "illyasviel von einzbern",
                "shirou emiya"
            },
            Tags = new[]
            {
                "lolicon",
                "defloration",
                "sole female",
                "sole male"
            },
            Artist    = "sen",
            Parody    = "fate kaleid liner prisma illya",
            PageCount = 33
        };
    }
}