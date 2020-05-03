using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace nhitomi
{
    /// <summary>
    /// Sanitizes an array or dictionary of tags.
    /// </summary>
    public class SanitizedTagsAttribute : SanitizerAttribute
    {
        protected override object AfterSanitize(object value)
        {
            value = base.AfterSanitize(value);

            switch (value)
            {
                // dictionary of tags
                case IDictionary dict:
                    var keys  = new object[dict.Count];
                    var count = 0;

                    foreach (var key in dict.Keys) // dict immutable within foreach
                        keys[count++] = key;

                    foreach (var key in keys)
                    {
                        if (dict[key] is string[] tags)
                        {
                            tags = TagFormatter.Format(tags);

                            if (tags == null)
                                dict.Remove(key);
                            else
                                dict[key] = tags;
                        }
                    }

                    break;

                case string[] tags:
                    value = TagFormatter.Format(tags);
                    break;
            }

            return value;
        }
    }

    public static class TagFormatter
    {
        /// <summary>
        /// Formats an array of tags.
        /// </summary>
        public static string[] Format(string[] array)
        {
            if (array == null)
                return null;

            var set = new HashSet<string>(array.Length);

            foreach (var item in array)
            {
                var value = Format(item);

                if (value != null)
                    set.Add(value);
            }

            if (set.Count == 0)
                return null;

            array = set.ToArray();

            Array.Sort(array, StringComparer.Ordinal);

            return array;
        }

        static readonly Regex _nonAsciiRegex = new Regex(@"[^\u0020-\u007E]", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Formats a tag string.
        /// </summary>
        public static string Format(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return null;

            // lowercase
            tag = tag.ToLowerInvariant();

            // remove diacritics
            tag = RemoveDiacritics(tag);

            // remove non-ascii
            tag = _nonAsciiRegex.Replace(tag, "");

            // space-separated
            tag = tag.Replace('_', ' ');

            return ModelSanitizer.Sanitize(tag);
        }

        /// <summary>
        /// https://stackoverflow.com/a/249126
        /// </summary>
        static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var builder    = new StringBuilder(text.Length);

            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    builder.Append(c);
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}