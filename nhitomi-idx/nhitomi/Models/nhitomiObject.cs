using System;
using System.Text;

namespace nhitomi.Models
{
    public enum ObjectType
    {
        /// <summary>
        /// <see cref="User"/>
        /// </summary>
        User = 0,

        /// <summary>
        /// <see cref="Book"/>
        /// </summary>
        Book = 1,

        /// <summary>
        /// <see cref="Image"/>
        /// </summary>
        Image = 2,

        /// <summary>
        /// <see cref="Snapshot"/>
        /// </summary>
        Snapshot = 3,

        /// <summary>
        /// <see cref="Collection"/>
        /// </summary>
        Collection = 4
    }

    /// <summary>
    /// Represents a reference to a generic nhitomi object.
    /// </summary>
    public readonly struct nhitomiObject : IEquatable<nhitomiObject>
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public readonly ObjectType Type;

        /// <summary>
        /// Object ID.
        /// </summary>
        public readonly string Id;

        public nhitomiObject(ObjectType type, string id)
        {
            Type = type;
            Id   = string.IsNullOrEmpty(id) ? null : id;
        }

        public bool Equals(nhitomiObject other) => Type == other.Type && Id == other.Id;
        public override bool Equals(object obj) => obj is nhitomiObject other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Type, Id);
        public override string ToString() => $"{Type} {Id}";

        public static bool operator ==(nhitomiObject a, nhitomiObject b) => a.Equals(b);
        public static bool operator !=(nhitomiObject a, nhitomiObject b) => !a.Equals(b);

        /// <summary>
        /// Serializes this object in a compact format.
        /// </summary>
        public byte[] Serialize()
        {
            var id     = Id ?? "";
            var buffer = new byte[Encoding.UTF8.GetByteCount(id) + 1];

            buffer[0] = (byte) Type;
            Encoding.UTF8.GetBytes(id, new Span<byte>(buffer, 1, buffer.Length - 1));

            return buffer;
        }

        /// <summary>
        /// Deserializes the output of <see cref="Serialize"/>.
        /// </summary>
        public static nhitomiObject Deserialize(byte[] buffer)
            => new nhitomiObject((ObjectType) buffer[0], Encoding.UTF8.GetString(buffer, 1, buffer.Length - 1));
    }
}