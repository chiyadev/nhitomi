using System;
using System.Collections.Generic;

namespace nhitomi.Discord.Parsing
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ModuleAttribute : Attribute
    {
        public string Name { get; }

        public string Alias
        {
            get => Aliases == null || Aliases.Length == 0 ? null : Aliases[0];
            set => Aliases = new[] { value };
        }

        public string[] Aliases { get; set; }
        public bool IsPrefixed { get; set; } = true;

        public ModuleAttribute(string name)
        {
            Name = name;
            //Aliases = new[] { name[0].ToString() };
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