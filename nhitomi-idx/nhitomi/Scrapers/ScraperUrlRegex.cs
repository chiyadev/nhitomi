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

        public string Group { get; set; } = "id";

        public IEnumerable<string> MatchIds(string input, bool strict)
        {
            var regex = strict ? Strict : Lax;

            foreach (Match match in regex.Matches(input))
            {
                var group = match.Groups[Group];

                if (group.Success)
                    yield return group.Value;
            }
        }

        public bool IsMatch(string input) => Strict.IsMatch(input);
    }
}