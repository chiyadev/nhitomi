using System;
using System.Collections.Generic;

namespace nhitomi.Discord.Parsing
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public string Name { get; }
        public string[] Aliases { get; set; }

        public CommandAttribute(string name)
        {
            Name = name;
        }

        public string[] GetNames()
        {
            var list = new List<string>
            {
                Name
            };

            if (Aliases != null)
                list.AddRange(Aliases);

            return list.ToArray();
        }
    }
}