using System.Globalization;

namespace nhitomi.Localization
{
    public class LocalizationEnglish : Localization
    {
        public override CultureInfo Culture => new CultureInfo("en");

        public override DoujinMessageLocalization DoujinMessage { get; } = new DoujinMessageEnglish();
        public override DownloadMessageLocalization DownloadMessage { get; } = new DownloadMessageEnglish();
        public override HelpMessageLocalization HelpMessage { get; } = new HelpMessageEnglish();

        class DoujinMessageEnglish : DoujinMessageLocalization
        {
        }

        class DownloadMessageEnglish : DownloadMessageLocalization
        {
        }

        class HelpMessageEnglish : HelpMessageLocalization
        {
        }
    }
}