using System;
using System.Text;

namespace nhitomi.Models
{
    /// <summary>
    /// Represents a reference to an object in Nanoka that supports snapshots.
    /// </summary>
    public struct NanokaObject : IEquatable<NanokaObject>
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public readonly SnapshotTarget Type;

        /// <summary>
        /// Object ID.
        /// </summary>
        public readonly string Id;

        public NanokaObject(SnapshotTarget type, string id)
        {
            Type = type;
            Id   = id ?? "";
        }

        public bool Equals(NanokaObject other) => Type == other.Type && Id == other.Id;
        public override bool Equals(object obj) => obj is NanokaObject other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Type, Id);

        public override string ToString() => $"{Type} {Id}";

        public static bool operator ==(NanokaObject a, NanokaObject b) => a.Equals(b);
        public static bool operator !=(NanokaObject a, NanokaObject b) => !a.Equals(b);

        /// <summary>
        /// Serializes this object in a compact format.
        /// </summary>
        public byte[] Serialize()
        {
            var buffer = new byte[Encoding.UTF8.GetByteCount(Id) + 1];

            buffer[0] = (byte) Type;
            Encoding.UTF8.GetBytes(Id, new Span<byte>(buffer, 1, buffer.Length - 1));

            return buffer;
        }

        /// <summary>
        /// Deserializes the output of <see cref="Serialize"/>.
        /// </summary>
        public static NanokaObject Deserialize(byte[] buffer)
            => new NanokaObject((SnapshotTarget) buffer[0], Encoding.UTF8.GetString(buffer, 1, buffer.Length - 1));
    }
}