using System;
using System.Collections.Generic;
using System.Linq;
using SmartFormat;

namespace nhitomi.Globalization
{
    public class LocalizationPath
    {
        readonly string[] _levels;

        public LocalizationPath(params string[] paths) : this((IEnumerable<string>) paths) { }

        public LocalizationPath(IEnumerable<string> paths) : this(string.Join('.', paths)) { }

        public LocalizationPath(string path)
        {
            _levels = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        }

        public LocalizationPath Up => new LocalizationPath(_levels.SkipLast(1));

        public LocalizationPath this[string key] => new LocalizationPath(_levels.Append(key));

        string FullPath => string.Join('.', _levels);

        public string this[Localization localization,
                           object variables = null]
        {
            get
            {
                var template = localization[FullPath];

                return variables == null ? template : Smart.Format(template, variables);
            }
        }

        public static implicit operator LocalizationPath(string path) => new LocalizationPath(path);
    }
}