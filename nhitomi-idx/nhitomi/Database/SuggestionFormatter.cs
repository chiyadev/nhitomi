using System.Collections.Generic;

namespace nhitomi.Database
{
    /// <summary>
    /// Takes a type and value and formats it as "{value}:{type}".
    /// This is used in the database where autocomplete suggestions are stored in a single "completion" field.
    /// </summary>
    public static class SuggestionFormatter
    {
        public static string Format(int type, string value) => $"{value}:{type}";

        public static IEnumerable<string> Format(Dictionary<int, string[]> items)
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

        public static (int?, string) Parse(string str)
        {
            var delimiter = str.LastIndexOf(':');

            if (delimiter == -1)
                return (null, str);

            var type  = str.Substring(delimiter + 1);
            var value = str.Substring(0, delimiter);

            return (int.Parse(type), value);
        }
    }
}