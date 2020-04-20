using System;
using System.Collections.Generic;
using StackExchange.Redis;

namespace nhitomi.Database
{
    /// <summary>
    /// Used for debugging.
    /// </summary>
    public class RedisKeyMemory : IDisposable
    {
        readonly HashSet<RedisKey> _keys = new HashSet<RedisKey>();

        public void Add(RedisKey s)
        {
            lock (_keys)
                _keys.Add(s);
        }

        public void Remove(RedisKey s)
        {
            lock (_keys)
                _keys.Remove(s);
        }

        public RedisKey[] Clear(RedisKey prefix)
        {
            lock (_keys)
            {
                var keys = _keys.ToArray(k => k.Prepend(prefix));

                Dispose();

                return keys;
            }
        }

        public void Dispose()
        {
            lock (_keys)
                _keys.Clear();
        }
    }
}