using System.Linq;
using System.Text.RegularExpressions;

namespace nhitomi
{
    public static class GalleryUtility
    {
        const string _nhentai =
            @"\b((https?:\/\/)?nhentai(\.net)?\/(g\/)?|nh\/)(?<src_nhentai>[0-9]{1,6})\b";

        const string _hitomi =
            @"\b((https?:\/\/)?hitomi(\.la)?\/(galleries\/)?|hi\/)(?<src_Hitomi>[0-9]{1,7})(\.html)?\b";

        const RegexOptions _patternOptions = RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase;

        static readonly string _pattern = $"({string.Join(")|(", _nhentai, _hitomi)})";

        static readonly Regex _regex = new Regex(_pattern,              _patternOptions);
        static readonly Regex _strictRegex = new Regex($"^{_pattern}$", _patternOptions);

        public static (string source, string id) Parse(string str)
        {
            var group = _strictRegex.Match(str)
                                    .Groups
                                    .FirstOrDefault(g => g.Success && g.Name != null && g.Name.StartsWith("src_"));

            return group == null
                ? default
                : (group.Name.Split('_', 2)[1], group.Value);
        }

        public static (string source, string id)[] ParseMany(string str)
        {
            // find successful groups starting with src_
            var groups = _regex.Matches(str)
                               .SelectMany(m => m.Groups)
                               .Where(g => g.Success && g.Name != null && g.Name.StartsWith("src_"));

            // remove src_ prefixes and return as tuple
            return groups.Select(g => (g.Name.Split('_', 2)[1], g.Value))
                         .ToArray();
        }

        public static string ExpandContraction(string source)
        {
            switch (source.Trim().ToLowerInvariant())
            {
                case "nh": return "nhentai";
                case "hi": return "hitomi";
                default:   return source;
            }
        }
    }
}