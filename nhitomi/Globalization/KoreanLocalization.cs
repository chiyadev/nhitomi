using System.Globalization;

namespace nhitomi.Globalization
{
    public class KoreanLocalization : Localization
    {
        protected override CultureInfo Culture { get; } = new CultureInfo("ko");

        protected override object CreateDefinition() => new
        {
            meta = new
            {
                translators = new[]
                {
                    "phosphene47"
                }
            },
            doujinMessage = new
            {
                language = "언어",
                parodyOf = "패러디",
                categories = "분류",
                character = "캐릭터",
                tags = "태그",
                contents = "내용"
            },
            downloadMessage = new
            {
                description = "위의 링크를 클릭해서 `{doujin.Name}`을(를) 다운로드 하실 수 있습니다."
            },
            helpMessage = new
            {
                title = "도움말",
                about = "nhitomi — 디스코드 동인지 로봇 by **chiya.dev**.",
                invite = "공식 서버: <{invite}>",
                doujin = new
                {
                    heading = "동인지"
                },
                collection = new
                {
                    heading = "콜렉션 관리"
                },
                source = new
                {
                    heading = "제공"
                },
                openSource = new
                {
                    heading = "공개 소프트웨어"
                }
            }
        };
    }
}