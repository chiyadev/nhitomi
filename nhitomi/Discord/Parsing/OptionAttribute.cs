using System;
using System.Collections.Generic;

namespace nhitomi.Discord.Parsing
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class OptionAttribute : Attribute
    {
        public string Name { get; }
        public char? Character { get; set; }

        public OptionAttribute(string name)
        {
            Name = name;
        }

        public string[] GetNames()
        {
            var list = new List<string>
            {
                $"-{Name}"
            };

            if (Character != null)
                list.Add($"-{Character}");

            return list.ToArray();
        }
    }
}