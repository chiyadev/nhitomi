using System.Globalization;

namespace nhitomi.Localization
{
    public class LocalizationKorean : Localization
    {
        public override CultureInfo Culture => new CultureInfo("ko");

        public override DoujinMessageLocalization DoujinMessage { get; } = new DoujinMessageKorean();
        public override DownloadMessageLocalization DownloadMessage { get; } = new DownloadMessageKorean();
        public override HelpMessageLocalization HelpMessage { get; } = new HelpMessageKorean();

        class DoujinMessageKorean : DoujinMessageLocalization
        {
            public override string Language => "언어";
            public override string ParodyOf => "패러디";
            public override string Categories => "분류";
            public override string Characters => "캐릭터";
            public override string Tags => "태그";
            public override string Contents => "내용";
        }

        class DownloadMessageKorean : DownloadMessageLocalization
        {
            public override string Description => "위의 링크를 클릭해서 `{doujin.Name}`을(를) 다운로드 하실 수 있습니다.";
        }

        class HelpMessageKorean : HelpMessageLocalization
        {
            public override string Title => "도움말";
            public override string About => "nhitomi — 디스코드 동인지 로봇 by **chiya.dev**.";
            public override string OfficialServer => "공식 서버: <{invite}>";

            public override string DoujinsHeading => "동인지";

            public override string CollectionsHeading => "콜렉션 관리";

            public override string SourcesHeading => "제공";

            public override string OpenSourceHeading => "공개 소프트웨어";
        }
    }
}