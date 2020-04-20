using System.Runtime.Serialization;

namespace nhitomi.Models
{
    // A minimal list of common languages.
    public enum LanguageType
    {
        [EnumMember(Value = "jp")] Japanese = 0,
        [EnumMember(Value = "en")] English = 1,
        [EnumMember(Value = "zh_hans")] ChineseSimplified = 2,
        [EnumMember(Value = "zh_hant")] ChineseTraditional = 3,
        [EnumMember(Value = "it")] Italian = 4,
        [EnumMember(Value = "es")] Spanish = 5,
        [EnumMember(Value = "hi")] Hindi = 6,
        [EnumMember(Value = "de")] German = 7,
        [EnumMember(Value = "fr")] French = 8,
        [EnumMember(Value = "tr")] Turkish = 9,
        [EnumMember(Value = "pl")] Polish = 10,
        [EnumMember(Value = "nl")] Dutch = 11,
        [EnumMember(Value = "ru")] Russian = 12,
        [EnumMember(Value = "ko")] Korean = 13,
        [EnumMember(Value = "in")] Indonesian = 14,
        [EnumMember(Value = "vi")] Vietnamese = 15
    }
}