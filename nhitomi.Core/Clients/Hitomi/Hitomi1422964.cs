using System;

namespace nhitomi.Core.Clients.Hitomi
{
    public class Hitomi1422964 : ClientTestCase
    {
        public override string DoujinId => "1422964";
        public override Type ClientType => typeof(Hitomi);

        public override DoujinInfo KnownValue { get; } = new DoujinInfo
        {
            GalleryUrl = "https://hitomi.la/galleries/1422964.html",
            PrettyName =
                "Nama Emo - Muboubi Na JC Pri Chan Idol No Oshiego No Tame Ni Otona Chinpo De Torotoro Asedaku Wakarase Koubi Shidou!",
            OriginalName = "생에모 무방비한 J○ 프리챤 아이돌 제자에게 어른 자지로 찐득찐득 땀 범벅이 되는 교미 지도!",
            //todo: check timezone
            //UploadTime = DateTime.Parse("2019-05-30T10:25:19+00:00").ToUniversalTime()
            Artist = "tokomaya keita",
            Group = "circle tokomaya",
            Language = "korean",
            Parody = "kiratto pri chan",
            Characters = new[]
            {
                "emo moegi"
            },
            Tags = new[]
            {
                "ahegao",
                "loli",
                "sole female",
                "twintails",
                "unusual pupils",
                "sole male"
            }
        };
    }
}
