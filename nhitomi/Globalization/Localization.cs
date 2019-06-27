using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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

        public static IEnumerable<Localization> GetAllLocalizations() => _localizations.Values
                                                                                       .GroupBy(l => l.Culture)
                                                                                       .Select(g => g.First());

        public static bool IsAvailable(string culture) => culture != null && _localizations.ContainsKey(culture);

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