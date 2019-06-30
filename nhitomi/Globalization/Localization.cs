using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SmartFormat;

namespace nhitomi.Globalization
{
    public abstract class Localization
    {
        static readonly Dictionary<string, Localization> _localizations = new Dictionary<string, Localization>();

        static Localization()
        {
            var types = typeof(Startup)
                       .Assembly
                       .GetTypes()
                       .Where(t => t.IsClass &&
                                   !t.IsAbstract &&
                                   t.IsSubclassOf(typeof(Localization)));

            foreach (var localization in types.Select(t => (Localization) Activator.CreateInstance(t)))
            {
                _localizations[localization.Culture.Name.ToLowerInvariant()]        = localization;
                _localizations[localization.Culture.EnglishName.ToLowerInvariant()] = localization;
            }
        }

        public static Localization Default => GetLocalization("en");

        public static Localization GetLocalization(string culture) =>
            culture != null && _localizations.TryGetValue(culture.ToLowerInvariant(), out var localization)
                ? localization
                : Default; // default to English if not found

        public static IEnumerable<Localization> GetAllLocalizations() =>
            // distinct by culture
            _localizations.Values
                          .GroupBy(l => l.Culture)
                          .Select(g => g.First());

        public static bool IsAvailable(string culture) => culture != null && _localizations.ContainsKey(culture);

        public abstract CultureInfo Culture { get; }
        protected virtual CultureInfo FallbackCulture => Default.Culture;

        public LocalizationEntry this[string key] => new LocalizationEntry(this, key, null);

        protected Localization()
        {
            _dict = new Lazy<LocalizationDictionary>(LoadDictionary);
        }

        LocalizationDictionary LoadDictionary()
        {
            var fallback = this == Default // don't fallback to ourselves; stack overflow
                ? null
                : GetLocalization(FallbackCulture?.Name);

            var dict = new LocalizationDictionary(fallback?._dict.Value);

            dict.AddDefinition(CreateDefinition());

            return dict;
        }

        protected abstract object CreateDefinition();

        public string GetTemplate(string key) => _dict[key];
    }

    public class LocalizationEntry
    {
        readonly Localization _localization;
        readonly string _key;
        readonly object _args;

        public LocalizationEntry(Localization localization,
                                 string key,
                                 object args)
        {
            _localization = localization;
            _key          = key;
            _args         = args;
        }

        public LocalizationEntry this[string key] => new LocalizationEntry(_localization,  $"{_key}.{key}", _args);
        public LocalizationEntry this[object args] => new LocalizationEntry(_localization, _key,            args);

        public override string ToString() => Smart.Format(_localization.GetTemplate(_key), _args);

        public static implicit operator string(LocalizationEntry entry) => entry.ToString();
    }
}