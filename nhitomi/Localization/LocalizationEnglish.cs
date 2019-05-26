using System.Globalization;

namespace nhitomi.Localization
{
    public class LocalizationEnglish : Localization
    {
        public override CultureInfo Culture => new CultureInfo("en");
    }
}