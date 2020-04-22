using System.Runtime.Serialization;

namespace nhitomi.Models
{
    /// <summary>
    /// Minimal list of common languages. Use ISO 639-1.
    /// </summary>
    public enum LanguageType
    {
        [EnumMember(Value = "ja")] Japanese = 0,
        [EnumMember(Value = "en")] English = 1,
        [EnumMember(Value = "zh_hans")] ChineseSimplified = 2,
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
}