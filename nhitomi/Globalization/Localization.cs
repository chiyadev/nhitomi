using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace nhitomi.Globalization
{
    public abstract class Localization
    {
        static readonly Dictionary<string, Localization> _localizations = typeof(Startup).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && t.IsClass && t.IsSubclassOf(typeof(Localization)))
            .Select(t => (Localization) Activator.CreateInstance(t))
            .ToDictionary(l => l.Culture.Name);

        public static Localization Default => GetLocalization("en");

        public static Localization GetLocalization(string culture) =>
            // default to English if not found
            culture != null && _localizations.TryGetValue(culture, out var localization)
                ? localization
                : Default;

        public static IEnumerable<Localization> GetAllLocalizations() => _localizations.Values;

        public static bool IsAvailable(string culture) =>
            culture != null && _localizations.ContainsKey(culture);

        readonly Lazy<LocalizationDictionary> _dict;

        public abstract CultureInfo Culture { get; }
        protected virtual CultureInfo FallbackCulture => Default.Culture;

        public string this[string key] => _dict.Value[key];

        protected Localization()
        {
            _dict = new Lazy<LocalizationDictionary>(LoadDictionary);
        }

        LocalizationDictionary LoadDictionary()
        {
            var fallback = this == Default
                ? null // no fallback to ourselves
                : GetLocalization(FallbackCulture?.Name);

            var dict = new LocalizationDictionary(fallback?._dict.Value);

            dict.AddDefinition(CreateDefinition());

            return dict;
        }

        protected abstract object CreateDefinition();
    }
}