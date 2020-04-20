using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace nhitomi.Localization
{
    /// <summary>
    /// Contains localized strings which can be formatted.
    /// </summary>
    public interface ILocale
    {
        static readonly object _emptyArgs = new object();

        /// <summary>
        /// Culture name of this locale.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the localized string associated with the specified key.
        /// </summary>
        string this[string key] => this[key, _emptyArgs];

        /// <summary>
        /// Formats a localized string associated with the specified key, using values from the given anonymous object.
        /// </summary>
        string this[string key, object args] { get; }

        /// <summary>
        /// Selector object that can be used to create locale sections.
        /// </summary>
        ILocaleSectionSelector Sections => new SectionSelector(this);

        sealed class Section : ILocale
        {
            readonly ILocale _locale;
            readonly string _prefix;

            public Section(ILocale locale, string prefix)
            {
                _locale = locale;
                _prefix = prefix;
            }

            public string Name => _locale.Name;
            public string this[string key, object args] => _locale[$"{_prefix}.{key}", args];
        }

        sealed class SectionSelector : ILocaleSectionSelector
        {
            readonly ILocale _locale;

            public SectionSelector(ILocale locale)
            {
                _locale = locale;
            }

            public ILocale this[string name] => new Section(_locale, name);
        }
    }

    /// <summary>
    /// Utility interface to create locale sections.
    /// </summary>
    public interface ILocaleSectionSelector
    {
        /// <summary>
        /// Gets a wrapper locale that prepends the given prefix to keys.
        /// </summary>
        /// <param name="name">Key prefix, excluding the ending separator. Separator is a dot '.' character.</param>
        ILocale this[string name] { get; }
    }

    /// <summary>
    /// Implements <see cref="ILocale"/> with one or more nested locales that are layered on top of each other.
    /// </summary>
    public class LayeredLocale : ILocale
    {
        readonly ILocale[] _locales;

        /// <summary>
        /// Constructs a new <see cref="LayeredLocale"/>.
        /// </summary>
        /// <param name="locales">Locales will be iterated in the order given and the first non-null value will be returned.</param>
        public LayeredLocale(params ILocale[] locales) : this((IEnumerable<ILocale>) locales) { }

        public LayeredLocale(IEnumerable<ILocale> locales)
        {
            _locales = locales.Where(l => l != null).ToArray();
        }

        public string Name => _locales.Length == 0 ? null : _locales[0].Name;

        public string this[string key, object args]
        {
            get
            {
                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < _locales.Length; i++)
                {
                    var value = _locales[i][key, args];

                    if (value != null)
                        return value;
                }

                return null;
            }
        }
    }

    public class NullLocale : ILocale
    {
        public string Name => null;
        public string this[string key, object args] => null;
    }

    /// <summary>
    /// Provides locale objects.
    /// </summary>
    public static class Locales
    {
        static readonly ConcurrentDictionary<string, ILocale> _cache = new ConcurrentDictionary<string, ILocale>();

        /// <summary>
        /// An empty locale that always returns null.
        /// </summary>
        public static ILocale Null { get; } = new NullLocale();

        /// <summary>
        /// Name of the default locale which is always "en-US".
        /// </summary>
        public static string DefaultName { get; } = "en-US";

        /// <summary>
        /// Default locale determined by <see cref="DefaultName"/>.
        /// </summary>
        public static ILocale Default => Get(DefaultName);

        /// <summary>
        /// Retrieves a locale by its culture name.
        /// </summary>
        public static ILocale Get(string name)
        {
            if (_cache.TryGetValue(name, out var locale))
                return locale;

            locale = new MessageFormatterLocale(name);

            // layer locales over the default locale
            if (locale.Name != DefaultName)
                locale = new LayeredLocale(locale, Default);

            return _cache[name] = locale;
        }
    }
}