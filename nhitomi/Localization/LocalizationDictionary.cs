using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace nhitomi.Localization
{
    public class LocalizationDictionary : IReadOnlyDictionary<string, string>
    {
        readonly LocalizationDictionary _fallback;

        readonly Dictionary<string, string> _dict = new Dictionary<string, string>();

        public LocalizationDictionary(LocalizationDictionary fallback = null)
        {
            _fallback = fallback;
        }

        static string FixKey(string key) => key.ToLowerInvariant();

        public void AddDefinition(object obj, string prefix = null)
        {
            foreach (var property in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var type = property.PropertyType;

                if (type == typeof(string))
                {
                    // add string property
                    _dict[FixKey(prefix + property.Name)] = (string) property.GetValue(obj);
                }
                else if (type.IsArray && type.GetElementType() == typeof(string))
                {
                    var values = (string[]) property.GetValue(property);

                    // join values as list
                    _dict[FixKey(prefix + property.Name)] = string.Join(", ", values);
                }
                else if (type.IsClass)
                {
                    // recurse on complex types
                    AddDefinition(property.GetValue(obj), $"{prefix}{property.Name}.");
                }
                else
                {
                    throw new ArgumentException($"Could not convert property {type.Name} ({type}) of definition obj.");
                }
            }
        }

        string IReadOnlyDictionary<string, string>.this[string key] => GetValue(key);
        public LocalizationCategory this[string key] => new LocalizationCategory(this, key);

        public int Count => _dict.Count;

        public IEnumerable<string> Keys => _dict.Keys.Select(FixKey);
        public IEnumerable<string> Values => _dict.Values;

        public bool ContainsKey(string key) => _dict.ContainsKey(FixKey(key));

        public string GetValue(string key) => TryGetValue(FixKey(key), out var value) ? value : $"`{key}`";

        public bool TryGetValue(string key, out string value)
        {
            if (_dict.TryGetValue(FixKey(key), out value))
                return true;

            // fallback to parent
            return _fallback != null && _fallback.TryGetValue(key, out value);
        }

        /// <inheritdoc />
        /// <summary>
        /// This will only enumerate definitions within this localization (i.e. no fallback).
        /// </summary>
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

        public LocalizationCategory this[string key] =>
            new LocalizationCategory(_dict, key, this);

        public LocalizationCategory this[params string[] keys] =>
            new LocalizationCategory(_dict, string.Join('.', keys), this);

        public override string ToString() => _dict.GetValue(_path);

        public static implicit operator string(LocalizationCategory category) => category.ToString();
    }
}