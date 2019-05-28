using System;
using System.Linq;

namespace nhitomi.Discord.Commands
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ModuleAttribute : Attribute
    {
        public string[] Path { get; }

        public ModuleAttribute(string name)
        {
            Path = name
                .Split('.', StringSplitOptions.RemoveEmptyEntries)
                .Select(n => n.Trim().ToLowerInvariant())
                .ToArray();
        }
    }
}
