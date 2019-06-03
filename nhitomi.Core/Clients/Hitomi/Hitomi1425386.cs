using System;

namespace nhitomi.Core.Clients.Hitomi
{
    public class Hitomi1425386 : ClientTestCase
    {
        public override string DoujinId => "1425386";
        public override Type ClientType => typeof(HitomiClient);

        public override DoujinInfo KnownValue { get; } = new DoujinInfo
        {
            GalleryUrl = "https://hitomi.la/galleries/1425386.html",
            PrettyName = "ALGOLAGNIA",
            OriginalName = "ALGOLAGNIA",
            Artist = "ukyo rst",
            UploadTime = new DateTime(2019, 6, 3, 5, 18, 0, DateTimeKind.Utc),
            Group = "u.m.e.project",
            Language = "chinese",
            Parody = "touhou project",
            Characters = new[]
            {
                "fujiwara no mokou",
                "keine kamishirasawa"
            },
            Tags = new[]
            {
                "collar",
                "females only",
                "yuri"
            },
            PageCount = 30
        };
    }
}
