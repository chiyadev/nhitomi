using System;
using System.Threading;

namespace nhitomi.Core
{
    public class AtomicCounter
    {
        int _current;

        public int Value => _current;

        public int Increment(int count = 1) => Interlocked.Add(ref _current, count);
        public int Decrement(int count = 1) => Increment(-count);

        public int Reset() => Interlocked.Exchange(ref _current, 0);

        sealed class AtomicCounterContext : IDisposable
        {
            readonly AtomicCounter _counter;

            public AtomicCounterContext(AtomicCounter counter)
            {
                _counter = counter;
            }

            public void Dispose() => _counter.Decrement();
        }

        public IDisposable Enter(out int value)
        {
            value = Increment();

            return new AtomicCounterContext(this);
        }

        public override string ToString() => Value.ToString();
    }
}
