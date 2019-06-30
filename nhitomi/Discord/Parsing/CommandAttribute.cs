using System;
using System.Collections.Generic;

namespace nhitomi.Discord.Parsing
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public string Name { get; }

        public string Alias
        {
            get => Aliases == null || Aliases.Length == 0 ? null : Aliases[0];
            set => Aliases = new[] { value };
        }

        public string[] Aliases { get; set; }

        /// <summary>
        /// Whether the name should be bound automatically.
        /// </summary>
        public bool BindName { get; set; } = true;

        public CommandAttribute(string name)
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