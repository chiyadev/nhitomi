using System;
using System.Buffers.Binary;
using System.IO;

namespace nhitomi
{
    public static class English
    {
        static readonly uint _seed;
        static readonly uint[] _hashes;

        static English()
        {
            // load pre-hashed pre-sorted lowercase dictionary
            Span<byte> buffer = File.ReadAllBytes("English.bin");

            _seed   = BinaryPrimitives.ReadUInt32BigEndian(buffer);
            _hashes = new uint[BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(4))];

            for (var i = 0; i < _hashes.Length; i++)
                _hashes[i] = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(8 + i * 4));
        }

        /// <summary>
        /// Returns true if the given string is a valid English word.
        /// </summary>
        public static bool IsEnglish(string word)
        {
            if (string.IsNullOrEmpty(word))
                return false;

            var hash = FastMurmur.Hash(word.ToLowerInvariant(), _seed);

            // we use binary search instead of hashset to lessen memory usage
            return Array.BinarySearch(_hashes, hash) >= 0;
        }
    }
}