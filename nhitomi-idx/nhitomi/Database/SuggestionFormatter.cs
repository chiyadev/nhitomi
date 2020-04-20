using System;
using System.Collections.Generic;

namespace nhitomi.Database
{
    /// <summary>
    /// Takes a type and value and formats it as "{value}:{type}".
    /// This is used in the database where autocomplete suggestions are stored in a single "completion" field.
    /// </summary>
    public static class SuggestionFormatter
    {
        public static string Format<T>(T type, string value) where T : struct, Enum
            => $"{value}:{CastTo<int>.Cast(type)}";

        public static IEnumerable<string> Format<T>(params (T type, string[] values)[] items) where T : struct, Enum
        {
            foreach (var (type, values) in items)
            {
                if (values == null)
                    continue;

                foreach (var value in values)
                {
                    if (string.IsNullOrEmpty(value))
                        continue;

                    yield return Format(type, value);
                }
            }
        }

        public static (T, string) Parse<T>(string str) where T : struct, Enum
        {
            var delimiter = str.LastIndexOf(':');

            if (delimiter == -1)
                return (default, str);

            var type  = str.Substring(delimiter + 1);
            var value = str.Substring(0, delimiter);

            return (CastTo<T>.Cast(int.Parse(type)), value);
        }
    }
}