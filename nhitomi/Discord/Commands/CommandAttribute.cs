using System;
using System.Text.RegularExpressions;

namespace nhitomi.Discord.Commands
{
    [AttributeUsage(AttributeTargets.Constructor)]
    public class CommandAttribute : Attribute
    {
        public Regex Pattern { get; }

        public CommandAttribute(string pattern, RegexOptions options = RegexOptions.IgnoreCase)
        {
            Pattern = new Regex(pattern, options | RegexOptions.Compiled | RegexOptions.Singleline);
        }
    }
}