using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace nhitomi.Localization
{
    public class LocalizationDictionary : IReadOnlyDictionary<string, string>
    {
        readonly Dictionary<string, string> _dict = new Dictionary<string, string>();

        public LocalizationDictionary(object obj)
        {
            AddDefinition(obj);
        }

        static string FixKey(string key) => key.ToLowerInvariant();

        void AddDefinition(object obj, string prefix = null)
        {
            foreach (var property in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.PropertyType == typeof(string))
                {
                    // add string property
                    _dict[prefix + FixKey(property.Name)] = (string) property.GetValue(obj);
                }
                else
                {
                    // recurse on complex types
                    AddDefinition(property.GetValue(obj), $"{prefix}{FixKey(property.Name)}.");
                }
            }
        }

        string IReadOnlyDictionary<string, string>.this[string key] =>
            TryGetValue(FixKey(key), out var value) ? value : $"`{key}`";

        public LocalizationCategory this[string key] => new LocalizationCategory(this, key);

        public int Count => _dict.Count;

        public IEnumerable<string> Keys => _dict.Keys.Select(FixKey);
        public IEnumerable<string> Values => _dict.Values;

        public bool ContainsKey(string key) => _dict.ContainsKey(FixKey(key));
        public bool TryGetValue(string key, out string value) => _dict.TryGetValue(FixKey(key), out value);

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() =>
            _dict.Select(x => new KeyValuePair<string, string>(FixKey(x.Key), x.Value)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public sealed class LocalizationCategory
    {
        readonly LocalizationDictionary _dict;
        readonly string _path;

        public LocalizationCategory(LocalizationDictionary dict, string name, LocalizationCategory parent = null)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Value cannot be null or empty.", nameof(name));

            _dict = dict;
            _path = parent == null
                ? name
                : $"{parent._path}.{name}";
        }

        public LocalizationCategory this[string key] => new LocalizationCategory(_dict, key, this);

        public override string ToString() => _dict[_path];

        public static implicit operator string(LocalizationCategory category) => category.ToString();
    }
}