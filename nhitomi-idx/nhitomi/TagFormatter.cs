using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace nhitomi
{
    public static class TagFormatter
    {
        /// <summary>
        /// Formats an array of tags in-place.
        /// Duplicate elements will be replaced with null.
        /// </summary>
        public static string[] Format(string[] array)
        {
            if (array != null)
            {
                var tags = new HashSet<string>(array.Length);

                for (var i = 0; i < array.Length; i++)
                {
                    var value = Format(array[i]);

                    if (!tags.Add(value))
                        value = null;

                    array[i] = value;
                }

                // sort tags
                Array.Sort(array);
            }

            return array;
        }

        static readonly Regex _asciiRegex = new Regex(@"[^\u0020-\u007E]", RegexOptions.Compiled | RegexOptions.Singleline);

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
            tag = _asciiRegex.Replace(tag, "");

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