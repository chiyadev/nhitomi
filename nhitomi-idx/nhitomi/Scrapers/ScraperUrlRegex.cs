using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace nhitomi.Scrapers
{
    public class ScraperUrlRegex
    {
        public readonly string Pattern;

        public readonly Regex Strict;
        public readonly Regex Lax;

        public ScraperUrlRegex(string pattern)
        {
            Pattern = pattern;

            Strict = new Regex($@"^{pattern}$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Lax    = new Regex($@"\b{pattern}\b", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        }

        public IEnumerable<Match> Match(string input, bool strict)
        {
            var regex = strict ? Strict : Lax;

            foreach (Match match in regex.Matches(input))
            {
                if (match.Success)
                    yield return match;
            }
        }
    }
}