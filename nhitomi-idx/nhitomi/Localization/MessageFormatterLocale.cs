using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jeffijoe.MessageFormat;
using Newtonsoft.Json.Linq;
using nhitomi.Models;

namespace nhitomi.Localization
{
    public class MessageFormatterLocale : ILocale
    {
        readonly MessageFormatter _formatter;
        readonly IReadOnlyDictionary<string, string> _dict;

        public LanguageType Type { get; }

        public string this[string key, object args]
        {
            get
            {
                var value = _dict.GetValueOrDefault(key);

                if (value == null)
                    return null;

                return _formatter.FormatMessage(value, args);
            }
        }

        public MessageFormatterLocale(LanguageType type)
        {
            Type = type;

            _formatter = new MessageFormatter(true, type.GetEnumName());

            // locales are in the same folder as this type
            using var stream = GetType().Assembly.GetManifestResourceStream($"{GetType().Namespace}.{_formatter.Locale}.json");

            if (stream == null)
            {
                _dict = new Dictionary<string, string>();
            }
            else
            {
                using var reader = new StreamReader(stream);

                _dict = JObject.Parse(reader.ReadToEnd())
                               .Descendants()
                               .Where(p => !p.Any())
                               .Aggregate(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), (dict, token) =>
                                {
                                    dict.Add(token.Path, token.ToString());
                                    return dict;
                                });
            }
        }
    }
}