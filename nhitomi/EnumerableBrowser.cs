// Copyright (c) 2018-2019 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace nhitomi
{
    public class EnumerableBrowser<T> : IAsyncEnumerator<T>, IDisposable
    {
        readonly IAsyncEnumerator<T> _enumerator;
        readonly Dictionary<int, T> _dict = new Dictionary<int, T>();

        public T Current => _dict[Index];
        public int Index { get; private set; } = -1;

        public EnumerableBrowser(IAsyncEnumerator<T> enumerator)
        {
            _enumerator = enumerator;
        }

        public async Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            if (_dict.ContainsKey(Index + 1))
            {
                ++Index;
                return true;
            }

            if (await _enumerator.MoveNext(cancellationToken))
            {
                _dict[++Index] = _enumerator.Current;
                return true;
            }

            return false;
        }

        public bool MovePrevious()
        {
            if (_dict.ContainsKey(Index - 1))
            {
                --Index;
                return true;
            }

            return false;
        }

        public void Reset() => Index = -1;

        public void Dispose() => _enumerator.Dispose();
    }
}