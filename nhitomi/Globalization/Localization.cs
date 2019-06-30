using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SmartFormat;

namespace nhitomi.Globalization
{
    public sealed class Localization
    {
        static readonly Dictionary<string, Localization> _localizations = new Dictionary<string, Localization>();

        public static Localization Default { get; }

        public static Localization GetLocalization(string culture) =>
            culture != null && _localizations.TryGetValue(culture.ToLowerInvariant(), out var localization)
                ? localization
                : Default;

        public static IEnumerable<Localization> GetAllLocalizations() =>
            // distinct by culture
            _localizations.Values
                          .GroupBy(l => l.Culture)
                          .Select(g => g.First());

        const string _defaultCulture = "en";

        static Localization()
        {
            var assembly = typeof(Startup).Assembly;

            var langNamespace = typeof(Localization).Namespace;
            var langResources = assembly.GetManifestResourceNames()
                                        .Where(n => n.StartsWith(langNamespace));

            foreach (var langResource in langResources)
            {
                // filename without extension
                var culture = new CultureInfo(Path.GetFileNameWithoutExtension(langResource).Split('.').Last());

                // load json as dictionary
                var dict = new LocalizationDictionary(culture);

                using (var stream = assembly.GetManifestResourceStream(langResource))
                using (var reader = new StreamReader(stream))
                using (var jsonReader = new JsonTextReader(reader))
                    dict.AddDefinition(JObject.Load(jsonReader));

                var localization = new Localization(culture, dict);

                _localizations[localization.Culture.Name.ToLowerInvariant()]        = localization;
                _localizations[localization.Culture.EnglishName.ToLowerInvariant()] = localization;
            }

            // default localization is English
            if (_localizations.TryGetValue(_defaultCulture, out var en))
            {
                Default = en;

                // set other localizations to fall back to English
                foreach (var localization in _localizations.Values.Where(l => l.Culture.Name != _defaultCulture))
                    localization.Dictionary.AddFallback(Default.Dictionary);
            }
            else
            {
                throw new FileNotFoundException($"Default localization culture '{_defaultCulture}' was not found,");
            }
        }

        public static bool IsAvailable(string culture) => culture != null && _localizations.ContainsKey(culture);

        public CultureInfo Culture { get; }
        public LocalizationDictionary Dictionary { get; }

        public LocalizationAccess this[string key,
                                       object args = null] => new LocalizationAccess(this, key, args);

        Localization(CultureInfo culture,
                     LocalizationDictionary dict)
        {
            Culture    = culture;
            Dictionary = dict;
        }
    }

    public class LocalizationAccess
    {
        public Localization Localization { get; }

        readonly string _key;
        readonly object _args;

        public LocalizationAccess(Localization localization,
                                  string key,
                                  object args)
        {
            Localization = localization;
            _key         = key;
            _args        = args;
        }

        public LocalizationAccess this[string key,
                                       object args = null] =>
            new LocalizationAccess(Localization, $"{_key}.{key}", args ?? _args);

        public override string ToString() => Smart.Format(Localization.Dictionary[_key], _args);

        public static implicit operator string(LocalizationAccess access) => access.ToString();
    }
}