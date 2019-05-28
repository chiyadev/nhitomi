using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace nhitomi.Localization
{
    public abstract class Localization
    {
        static readonly Dictionary<string, Localization> _localizations = typeof(Startup).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && t.IsClass && t.IsSubclassOf(typeof(Localization)))
            .Select(t => (Localization) Activator.CreateInstance(t))
            .ToDictionary(l => l.Culture.Name);

        public static Localization GetLocalization(string culture) =>
            // ReSharper disable once TailRecursiveCall
            // default to English if not found
            _localizations.TryGetValue(culture, out var localization) ? localization : GetLocalization("en");

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
            var fallback = GetLocalization(FallbackCulture?.Name);
            var dict = new LocalizationDictionary(fallback?._dict.Value);

            dict.AddDefinition(CreateDefinition());

            return dict;
        }

        protected abstract object CreateDefinition();
    }
}