using System;

namespace nhitomi.Discord.Parsing
{
    [AttributeUsage(AttributeTargets.Method)]
    public class BindingAttribute : Attribute
    {
        public string Expression { get; }

        public BindingAttribute(string expression)
        {
            Expression = expression;
        }
    }
}