using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace nhitomi.Localization
{
    public abstract class Localization
    {
        static readonly Dictionary<CultureInfo, Localization> _localizations = typeof(Startup).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && t.IsClass && t.IsSubclassOf(typeof(Localization)))
            .Select(t => (Localization) Activator.CreateInstance(t))
            .ToDictionary(l => l.Culture);

        public static Localization GetLocalization(CultureInfo culture) =>
            _localizations.TryGetValue(culture, out var localization) ? localization : null;

        readonly Lazy<LocalizationDictionary> _dict;

        protected abstract CultureInfo Culture { get; }
        protected abstract CultureInfo FallbackCulture { get; }

        public LocalizationCategory this[string key] => _dict.Value[key];

        protected Localization()
        {
            _dict = new Lazy<LocalizationDictionary>(LoadDictionary);
        }

        LocalizationDictionary LoadDictionary()
        {
            var fallback = GetLocalization(FallbackCulture);
            var dict = new LocalizationDictionary(fallback?._dict.Value);

            dict.AddDefinition(CreateDefinition());

            return dict;
        }

        protected abstract object CreateDefinition();
    }
}