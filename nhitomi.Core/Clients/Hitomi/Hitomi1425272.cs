using System;

namespace nhitomi.Core.Clients.Hitomi
{
    public class Hitomi1425272 : ClientTestCase
    {
        public override string DoujinId => "1425272";
        public override Type ClientType => typeof(HitomiClient);

        public override DoujinInfo KnownValue { get; } = new DoujinInfo
        {
            GalleryUrl = "https://hitomi.la/galleries/1425272.html",
            PrettyName = "Betsu ni Kimi to Blend Shitai Wake ja Nai kara ne...",
            OriginalName = "Betsu ni Kimi to Blend Shitai Wake ja Nai kara ne...",
            Artist = "staryume",
            UploadTime = new DateTime(2019, 6, 3, 0, 21, 0, DateTimeKind.Utc),
            Group = "star-dreamer tei",
            Language = "portuguese",
            Parody = "blend s",
            Characters = new[]
            {
                "kaho hinata"
            },
            Tags = new[]
            {
                "big breasts",
                "sole female",
                "stockings",
                "twintails",
                "condom",
                "sole male"
            },
            PageCount = 17
        };
    }
}
