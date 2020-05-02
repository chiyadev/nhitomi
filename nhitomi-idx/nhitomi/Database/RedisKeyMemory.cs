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

        public void Add(RedisKey key)
        {
            lock (_keys)
                _keys.Add(key);
        }

        public void Add(RedisKey[] keys)
        {
            lock (_keys)
            {
                foreach (var key in keys)
                    Add(key);
            }
        }

        public void Remove(RedisKey key)
        {
            lock (_keys)
                _keys.Remove(key);
        }

        public void Remove(RedisKey[] keys)
        {
            lock (_keys)
            {
                foreach (var key in keys)
                    Remove(key);
            }
        }

        public RedisKey[] Clear(RedisKey prefix = default)
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