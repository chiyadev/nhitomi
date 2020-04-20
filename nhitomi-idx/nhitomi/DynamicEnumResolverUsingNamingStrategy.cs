using System;
using MessagePack;
using MessagePack.Formatters;
using Newtonsoft.Json.Serialization;

namespace nhitomi
{
    /// <summary>
    /// Custom enum formatter which respects a specific <see cref="NamingStrategy"/>.
    /// </summary>
    public class DynamicEnumResolverUsingNamingStrategy : IFormatterResolver
    {
        readonly NamingStrategy _strategy;

        public DynamicEnumResolverUsingNamingStrategy(NamingStrategy strategy)
        {
            _strategy = strategy;
        }

        static bool UnwrapNullable(ref Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type);

            if (underlying == null)
                return false;

            type = underlying;
            return true;
        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            var type = typeof(T);

            if (UnwrapNullable(ref type))
            {
                if (!type.IsEnum)
                    return null;

                var formatter = this.GetFormatterDynamic(type);

                if (formatter == null)
                    return null;

                return (IMessagePackFormatter<T>) Activator.CreateInstance(typeof(StaticNullableFormatter<>).MakeGenericType(type), formatter);
            }

            if (!type.IsEnum)
                return null;

            return new InternalFormatter<T>(_strategy);
        }

        sealed class InternalFormatter<T> : IMessagePackFormatter<T>
        {
            readonly NamingStrategy _strategy;

            public InternalFormatter(NamingStrategy strategy)
            {
                _strategy = strategy;
            }

            public void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options)
                => writer.Write(_strategy.GetPropertyName(value.ToString(), false));

            public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
                => (T) Enum.Parse(typeof(T), reader.ReadString(), true);
        }
    }
}