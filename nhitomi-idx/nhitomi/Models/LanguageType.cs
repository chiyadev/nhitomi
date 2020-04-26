using System;
using System.Runtime.Serialization;

namespace nhitomi.Models
{
    /// <summary>
    /// Minimal list of common languages. Uses ISO 639-1.
    /// </summary>
    public enum LanguageType
    {
        [EnumMember(Value = "ja")] Japanese = 0,
        [EnumMember(Value = "en")] English = 1,

        /// <summary>
        /// Assume Mandarin, but expect any dialect of Chinese.
        /// This is the catch-all Chinese language used when the specific dialect cannot be determined.
        /// </summary>
        [EnumMember(Value = "zh")] Chinese = 2,

        /// <summary>
        /// Assume Cantonese.
        /// </summary>
        [EnumMember(Value = "zh_hant")] ChineseTraditional = 3,
        [EnumMember(Value = "ko")] Korean = 4,
        [EnumMember(Value = "it")] Italian = 5,
        [EnumMember(Value = "es")] Spanish = 6,
        [EnumMember(Value = "hi")] Hindi = 7,
        [EnumMember(Value = "de")] German = 8,
        [EnumMember(Value = "fr")] French = 9,
        [EnumMember(Value = "tr")] Turkish = 10,
        [EnumMember(Value = "nl")] Dutch = 11,
        [EnumMember(Value = "ru")] Russian = 12,
        [EnumMember(Value = "id")] Indonesian = 13,
        [EnumMember(Value = "vi")] Vietnamese = 14
    }

    public static class LanguageTypeExtensions
    {
        /// <summary>
        /// Attempts to parse this string as <see cref="LanguageType"/>.
        /// </summary>
        public static bool TryParseLocaleAsLanguage(this string s, out LanguageType lang)
        {
            foreach (LanguageType x in Enum.GetValues(typeof(LanguageType)))
            {
                if (s.StartsWith(x.GetEnumName()))
                {
                    lang = x;
                    return true;
                }
            }

            lang = default;
            return false;
        }
    }
}