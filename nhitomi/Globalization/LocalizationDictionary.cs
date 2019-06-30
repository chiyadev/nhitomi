using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace nhitomi.Globalization
{
    public class LocalizationDictionary : IReadOnlyDictionary<string, string>
    {
        readonly CultureInfo _culture;
        readonly List<LocalizationDictionary> _fallbacks = new List<LocalizationDictionary>();
        readonly Dictionary<string, string> _dict = new Dictionary<string, string>();

        public LocalizationDictionary(CultureInfo culture)
        {
            _culture = culture;
        }

        static string FixKey(string key) => key.ToLowerInvariant();

        public void AddFallback(LocalizationDictionary dict) => _fallbacks.Add(dict);

        public void AddDefinition(JObject obj,
                                  string prefix = null)
        {
            foreach (var property in obj.Properties())
            {
                var key = FixKey($"{prefix}{property.Name}");

                switch (property.Value)
                {
                    case JObject jObject:
                        // recurse on objects
                        AddDefinition(jObject, key + '.');
                        break;

                    case JArray jArray:
                        var jValues = jArray.Values<string>().ToArray();

                        // join array values using comma
                        _dict[key] = string.Join(", ", jValues);

                        // add each item as index
                        for (var i = 0; i < jValues.Length; i++)
                            _dict[$"{key}.{i}"] = jValues[i];

                        break;

                    case JValue jValue:
                        // add as string
                        _dict[key] = jValue.Value?.ToString() ?? "";
                        break;

                    default:
                        throw new NotSupportedException($"Unable to parse key '{key}' of type {property.Value.Type} " +
                                                        $"in localization '{_culture.Name}'.");
                }
            }
        }

        public string this[string key] => TryGetValue(FixKey(key), out var value) ? value : null;

        public int Count => _dict.Count;

        public IEnumerable<string> Keys => _dict.Keys.Select(FixKey);
        public IEnumerable<string> Values => _dict.Values;

        public bool ContainsKey(string key) => _dict.ContainsKey(FixKey(key)) ||
                                               _fallbacks.Any(d => d.ContainsKey(key));

        public bool TryGetValue(string key,
                                out string value)
        {
            if (_dict.TryGetValue(FixKey(key), out value))
                return true;

            foreach (var fallback in _fallbacks)
            {
                if (fallback.TryGetValue(key, out value))
                    return true;
            }

            return false;
        }

        Dictionary<string, string> GetFlattened()
        {
            var dict = new Dictionary<string, string>();

            foreach (var (key, value) in _dict)
                dict[key] = value;

            foreach (var fallback in _fallbacks)
            {
                foreach (var (key, value) in fallback.GetFlattened())
                    dict[key] = value;
            }

            return dict;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => GetFlattened().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}