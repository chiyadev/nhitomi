using System;
using System.Collections.Generic;
using System.Linq;
using SmartFormat;

namespace nhitomi.Globalization
{
    public delegate string LocalizationFormatter(object variables = null);

    public class LocalizationPath
    {
        readonly string[] _paths;

        public LocalizationPath(params string[] paths) : this((IEnumerable<string>) paths)
        {
        }

        public LocalizationPath(IEnumerable<string> paths)
        {
            _paths = string
                .Join('.', paths)
                .Split('.', StringSplitOptions.RemoveEmptyEntries);
        }

        public LocalizationPath Up => new LocalizationPath(_paths.SkipLast(1));

        public LocalizationPath this[string key] => new LocalizationPath(_paths.Append(key));

        string FullPath => string.Join('.', _paths);

        public LocalizationFormatter this[Localization localization]
        {
            get
            {
                var template = localization[FullPath];

                return v => v == null ? template : Smart.Format(template, v);
            }
        }
    }
}