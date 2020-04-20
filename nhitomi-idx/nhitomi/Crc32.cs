// Copyright (c) Damien Guard.  All rights reserved.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace nhitomi
{
    // taken from https://github.com/damieng/DamienGKit/blob/master/CSharp/DamienG.Library/Security/Cryptography/Crc32.cs

    /// <summary>
    /// Implements a 32-bit CRC hash algorithm compatible with Zip etc.
    /// </summary>
    /// <remarks>
    /// Crc32 should only be used for backward compatibility with older file formats
    /// and algorithms. It is not secure enough for new applications.
    /// If you need to call multiple times for the same data either use the HashAlgorithm
    /// interface or remember that the result of one Compute call needs to be ~ (XOR) before
    /// being passed in as the seed for the next Compute call.
    /// </remarks>
    public sealed class Crc32 : HashAlgorithm
    {
        public const uint DefaultPolynomial = 0xedb88320u;
        public const uint DefaultSeed = 0xffffffffu;

        readonly uint _seed;
        readonly uint[] _table;
        uint _hash;

        public Crc32() : this(DefaultPolynomial, DefaultSeed) { }

        public Crc32(uint polynomial, uint seed)
        {
            if (!BitConverter.IsLittleEndian)
                throw new PlatformNotSupportedException("Not supported on Big Endian processors");

            _table = InitializeTable(polynomial);
            _seed  = _hash = seed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Initialize()
            => _hash = _seed;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void HashCore(byte[] array, int ibStart, int cbSize)
            => _hash = CalculateHash(_table, _hash, array, ibStart, cbSize);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override byte[] HashFinal()
        {
            var hashBuffer = UInt32ToBigEndianBytes(~_hash);
            HashValue = hashBuffer;
            return hashBuffer;
        }

        public override int HashSize => 32;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Compute(byte[] buffer)
            => Compute(DefaultSeed, buffer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Compute(uint seed, byte[] buffer)
            => ~CalculateHash(_defaultTable, seed, buffer, 0, buffer.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Compute(uint polynomial, uint seed, byte[] buffer)
            => ~CalculateHash(InitializeTable(polynomial), seed, buffer, 0, buffer.Length);

        static uint[] _defaultTable = InitializeTable(DefaultPolynomial);

        static uint[] InitializeTable(uint polynomial)
        {
            if (polynomial == DefaultPolynomial && _defaultTable != null)
                return _defaultTable;

            var createTable = new uint[256];
            for (var i = 0; i < 256; i++)
            {
                var entry = (uint) i;
                for (var j = 0; j < 8; j++)
                {
                    if ((entry & 1) == 1)
                        entry = (entry >> 1) ^ polynomial;
                    else
                        entry >>= 1;
                }

                createTable[i] = entry;
            }

            if (polynomial == DefaultPolynomial)
                _defaultTable = createTable;

            return createTable;
        }

        static uint CalculateHash(uint[] table, uint seed, IList<byte> buffer, int start, int size)
        {
            var hash = seed;
            for (var i = start; i < start + size; i++)
                hash = (hash >> 8) ^ table[buffer[i] ^ hash & 0xff];
            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte[] UInt32ToBigEndianBytes(uint uint32)
        {
            var result = BitConverter.GetBytes(uint32);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(result);

            return result;
        }
    }
}