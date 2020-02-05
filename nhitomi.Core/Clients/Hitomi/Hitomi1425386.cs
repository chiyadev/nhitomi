using System;

namespace nhitomi.Core.Clients.Hitomi
{
    public class Hitomi1425386 : ClientTestCase
    {
        public override string DoujinId => "doujinshi/algolagnia-中文-1425386";
        public override Type ClientType => typeof(HitomiClient);

        public override DoujinInfo KnownValue { get; } = new DoujinInfo
        {
            PrettyName   = "ALGOLAGNIA",
            OriginalName = "ALGOLAGNIA",
            Artist       = "ukyo rst",
            UploadTime   = new DateTime(2019, 6, 3, 10, 18, 0, DateTimeKind.Utc),
            SourceId     = "doujinshi/algolagnia-中文-1425386",
            Group        = "u.m.e.project",
            Language     = "chinese",
            Parody       = "touhou project",
            Characters = new[]
            {
                "fujiwara no mokou",
                "keine kamishirasawa"
            },
            Tags = new[]
            {
                "bondage",
                "collar",
                "females only",
                "piercing",
                "sex toys",
                "yuri"
            },
            PageCount = 30
        };
    }
}