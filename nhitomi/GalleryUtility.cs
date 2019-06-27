using System.Linq;
using System.Text.RegularExpressions;

namespace nhitomi
{
    public static class GalleryUtility
    {
        const string _nhentai =
            @"\b((http|https):\/\/)?nhentai(\.net)?\/(g\/)?(?<src_nhentai>[0-9]{1,6})\b";

        const string _hitomi =
            @"\b((http|https):\/\/)?hitomi(\.la)?\/(galleries\/)?(?<src_Hitomi>[0-9]{1,7})\b";

        static readonly Regex _regex = new Regex(
            $"({string.Join(")|(", _nhentai, _hitomi)})",
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        public static (string source, string id)[] Parse(string str)
        {
            // find successful groups starting with src_
            var matches = _regex.Matches(str)
                                .SelectMany(m => m.Groups)
                                .Where(g => g.Success && g.Name != null && g.Name.StartsWith("src_"));

            // remove src_ prefixes and return as tuple
            return matches.Select(g => (g.Name.Split('_', 2)[1], g.Value))
                          .ToArray();
        }
    }
}
