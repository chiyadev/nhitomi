using System;
using System.Text;

namespace nhitomi
{
    public static class FastMurmur
    {
        public const uint Seed = 0x695e0677;

        static readonly Encoding _encoding = Encoding.UTF8;

        public static uint Hash(ReadOnlySpan<char> str, uint seed = Seed)
        {
            Span<byte> data = stackalloc byte[_encoding.GetByteCount(str)];

            _encoding.GetBytes(str, data);

            return Hash(data, seed);
        }

        public static uint Hash(ReadOnlySpan<byte> data, uint seed = Seed)
        {
            const uint m = 0x5bd1e995;
            const int  r = 24;

            var len = data.Length;

            if (len == 0)
                return 0;

            var h = seed ^ (uint) len;
            var i = 0;

            while (len >= 4)
            {
                var k = (uint) (data[i++] | (data[i++] << 8) | (data[i++] << 16) | (data[i++] << 24));

                k *= m;
                k ^= k >> r;
                k *= m;

                h *= m;
                h ^= k;

                len -= 4;
            }

            switch (len)
            {
                case 3:
                    h ^= (ushort) (data[i++] | (data[i++] << 8));
                    h ^= (uint) (data[i] << 16);
                    h *= m;
                    break;

                case 2:
                    h ^= (ushort) (data[i++] | (data[i] << 8));
                    h *= m;
                    break;

                case 1:
                    h ^= data[i];
                    h *= m;
                    break;
            }

            h ^= h >> 13;
            h *= m;
            h ^= h >> 15;

            return h;
        }
    }
}