using System;
using System.Collections.Generic;
using System.Linq;

namespace nhitomi
{
    public delegate T DependencyFactory<out T>(IServiceProvider services);

    public static class DependencyUtility
    {
        public static DependencyFactory<object> CreateFactory(Type type)
        {
            var constructor = type.GetConstructors().FirstOrDefault();

            if (constructor == null)
                throw new ArgumentException($"{type} does not have an injectable constructor");

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
                                $"Unable to resolve service for parameter '{parameter.name}' ({parameter.type}) while attempting to activate '{type}'.");

                        argument = parameter.defaultValue;
                    }

                    arguments[i] = argument;
                }

                return Activator.CreateInstance(type, arguments);
            };
        }
    }

    public static class DependencyUtility<T>
    {
        static readonly DependencyFactory<object> _factory = DependencyUtility.CreateFactory(typeof(T));

        public static DependencyFactory<T> Factory => s => (T) _factory(s);
    }

    public class ServiceDictionary : Dictionary<Type, object>, IServiceProvider
    {
        readonly IServiceProvider _fallback;

        public ServiceDictionary(IServiceProvider fallback = null)
        {
            _fallback = fallback;
        }

        public object GetService(Type serviceType) =>
            TryGetValue(serviceType, out var obj)
                ? obj
                : _fallback?.GetService(serviceType);
    }
}