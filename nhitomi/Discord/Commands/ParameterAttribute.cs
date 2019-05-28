using System;
using System.Text.RegularExpressions;

namespace nhitomi.Discord.Commands
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class ParameterAttribute : Attribute
    {
        public Regex Pattern { get; }

        public ParameterAttribute(string pattern, RegexOptions options = RegexOptions.IgnoreCase)
        {
            Pattern = new Regex(pattern, options | RegexOptions.Compiled | RegexOptions.Singleline);
        }
    }
}