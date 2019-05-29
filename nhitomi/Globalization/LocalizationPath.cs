using System;
using System.Collections.Generic;
using System.Linq;
using nhitomi.Discord;
using SmartFormat;

namespace nhitomi.Globalization
{
    public delegate string LocalizationFormatter(object variables = null);

    public class LocalizationPath
    {
        readonly string[] _levels;

        public LocalizationPath(params string[] paths) : this((IEnumerable<string>) paths)
        {
        }

        public LocalizationPath(IEnumerable<string> paths) : this(string.Join('.', paths))
        {
        }

        public LocalizationPath(string path)
        {
            _levels = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        }

        public LocalizationPath Up => new LocalizationPath(_levels.SkipLast(1));

        public LocalizationPath this[string key] => new LocalizationPath(_levels.Append(key));

        string FullPath => string.Join('.', _levels);

        public LocalizationFormatter this[Localization localization]
        {
            get
            {
                var template = localization[FullPath];

                return v => v == null ? template : Smart.Format(template, v);
            }
        }

        public LocalizationFormatter this[IDiscordContext context] => this[context.Localization];

        public static implicit operator LocalizationPath(string path) => new LocalizationPath(path);
    }
}