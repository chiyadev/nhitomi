using System;
using System.Linq;
using System.Reflection;

namespace nhitomi
{
    public delegate T DependencyFactory<out T>(IServiceProvider services);

    public static class DependencyExtensions
    {
        public static Type[] GetConcreteTypes<T>(this Assembly assembly) => assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && (t.IsClass || t.IsValueType) && typeof(T).IsAssignableFrom(t))
            .ToArray();

        public static DependencyFactory<T> GetDependencyFactory<T>()
        {
            var type = typeof(T);
            var constructor = type.GetConstructors().FirstOrDefault();

            if (constructor == null)
                throw new ArgumentException($"Type {type} does not have an injectable constructor");

            var parameters = constructor
                .GetParameters()
                .Select(p => new
                {
                    name = p.Name,
                    optional = p.IsOptional,
                    defaultValue = p.DefaultValue,
                    type = p.ParameterType
                })
                .ToArray();

            return s =>
            {
                var arguments = new object[parameters.Length];

                for (var i = 0; i < arguments.Length; i++)
                {
                    var parameter = parameters[i];
                    var argument = s.GetService(parameter.type);

                    if (argument == null)
                    {
                        if (!parameter.optional)
                            throw new InvalidOperationException(
                                $"Could not inject dependency {parameter.name} ({parameter.type}) of {type}.");

                        argument = parameter.defaultValue;
                    }

                    arguments[i] = argument;
                }

                return (T) Activator.CreateInstance(type, arguments);
            };
        }
    }
}