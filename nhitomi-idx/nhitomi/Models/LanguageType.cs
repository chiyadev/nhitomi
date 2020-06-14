using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;

namespace nhitomi.Models
{
    /// <summary>
    /// Minimal list of common languages. Uses ISO 639-1.
    /// </summary>
    public enum LanguageType
    {
        [EnumMember(Value = "ja-JP")] Japanese = 0,
        [EnumMember(Value = "en-US")] English = 1,

        /// <summary>
        /// Assume Mandarin, but expect any dialect of Chinese.
        /// </summary>
        [EnumMember(Value = "zh-CN")] Chinese = 2,
        [EnumMember(Value = "ko-KR")] Korean = 3,
        [EnumMember(Value = "it-IT")] Italian = 4,
        [EnumMember(Value = "es-ES")] Spanish = 5,
        [EnumMember(Value = "de-DE")] German = 6,
        [EnumMember(Value = "fr-FR")] French = 7,
        [EnumMember(Value = "tr-TR")] Turkish = 8,
        [EnumMember(Value = "nl-NL")] Dutch = 9,
        [EnumMember(Value = "ru-RU")] Russian = 10,
        [EnumMember(Value = "id-ID")] Indonesian = 11,
        [EnumMember(Value = "vi-VN")] Vietnamese = 12
    }

    public static class LanguageTypeExtensions
    {
        static readonly Dictionary<string, LanguageType> _map = new Dictionary<string, LanguageType>(StringComparer.OrdinalIgnoreCase);

        static LanguageTypeExtensions()
        {
            foreach (LanguageType lang in Enum.GetValues(typeof(LanguageType)))
            {
                var culture = CultureInfo.GetCultureInfo(lang.GetEnumName());

                do
                {
                    _map[culture.Name]                       = lang;
                    _map[culture.TwoLetterISOLanguageName]   = lang;
                    _map[culture.ThreeLetterISOLanguageName] = lang;
                    _map[culture.NativeName]                 = lang;
                    _map[culture.EnglishName]                = lang;
                    _map[culture.DisplayName]                = lang;

                    try
                    {
                        var region = new RegionInfo(culture.Name);

                        _map[region.Name]                     = lang;
                        _map[region.TwoLetterISORegionName]   = lang;
                        _map[region.ThreeLetterISORegionName] = lang;
                        _map[region.NativeName]               = lang;
                        _map[region.EnglishName]              = lang;

                        if (region.DisplayName != null)
                            _map[region.DisplayName] = lang;
                    }
                    catch (ArgumentException) { }

                    culture = culture.Parent;
                }
                while (!Equals(culture, CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Attempts to parse this string as <see cref="LanguageType"/>. Returns English on failure.
        /// </summary>
        public static LanguageType ParseAsLanguage(this string s) => _map.TryGetValue(s, out var lang) ? lang : LanguageType.English;
    }
}