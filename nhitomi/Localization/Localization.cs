using System.Globalization;

namespace nhitomi.Localization
{
    public abstract class Localization
    {
        public abstract CultureInfo Culture { get; }
        public abstract LocalizationDictionary Dictionary { get; }
    }
}