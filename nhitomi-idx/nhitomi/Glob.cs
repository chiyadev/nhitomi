using System;
using System.Text.RegularExpressions;

namespace nhitomi
{
    /// <summary>
    /// Represents a glob pattern that can be matched against.
    /// </summary>
    public class Glob : IEquatable<Glob>
    {
        public string Pattern { get; }
        public Regex Regex { get; }

        public Glob(string pattern, RegexOptions options = RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase)
        {
            Pattern = pattern;
            Regex   = new Regex($"^{Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".")}$", options);
        }

        public bool Match(string input) => Regex.IsMatch(input);

        public bool Equals(Glob other) => Pattern == other?.Pattern;
        public override bool Equals(object obj) => obj is Glob other && Equals(other);

        public override int GetHashCode() => Pattern.GetHashCode();
    }
}