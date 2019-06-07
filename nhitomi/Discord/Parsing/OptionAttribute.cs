using System;
using System.Collections.Generic;

namespace nhitomi.Discord.Parsing
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class OptionAttribute : Attribute
    {
        public string Name { get; }
        public char? Character { get; }

        public OptionAttribute(string name)
        {
            Name = name;
        }

        public OptionAttribute(string name, char character) : this(name)
        {
            Character = character;
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