using System;

namespace nhitomi.Discord.Commands
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ModuleAttribute : Attribute
    {
        public string Name { get; }

        public ModuleAttribute(string name)
        {
        }
    }
}