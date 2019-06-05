using System.Globalization;

namespace nhitomi.Globalization
{
    public class KoreanLocalization : Localization
    {
        public override CultureInfo Culture { get; } = new CultureInfo("ko");

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
                group = "그룹",
                parody = "패러디",
                categories = "분류",
                character = "캐릭터",
                tags = "태그",
                contents = "내용"
            },
            downloadMessage = new
            {
                description = "위의 링크를 클릭해서 `{doujin.OriginalName}`을(를) 다운로드 하실 수 있습니다."
            },
            helpMessage = new
            {
                title = "도움말",
                about = "디스코드 동인지 로봇",
                invite = "공식 서버: <{invite}>",
                doujins = new
                {
                    heading = "동인지"
                },
                collections = new
                {
                    heading = "콜렉션 관리"
                },
                sources = new
                {
                    heading = "제공"
                },
                translations = new
                {
                    heading = "번역",
                    text = "한국어 번역을 도와주신 분들: {translators}"
                },
                openSource = new
                {
                    heading = "공개 소프트웨어"
                }
            }
        };
    }
}