// Copyright (c) 2018-2019 chiya.dev
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace nhitomi
{
    public interface IEnumerableBrowser : IDisposable
    {
        int Index { get; }

        Task<bool> MoveNext(CancellationToken cancellationToken = default);
        bool MovePrevious();

        void Reset();
    }

    public class EnumerableBrowser<T> : IAsyncEnumerator<T>, IEnumerableBrowser
    {
        readonly IAsyncEnumerator<T> _enumerator;
        readonly Dictionary<int, T> _dict = new Dictionary<int, T>();

        public T Current => _dict[Index];
        public int Index { get; private set; } = -1;

        public EnumerableBrowser(IAsyncEnumerator<T> enumerator)
        {
            _enumerator = enumerator;
        }

        public async Task<bool> MoveNext(CancellationToken cancellationToken = default)
        {
            if (_dict.ContainsKey(Index + 1))
            {
                ++Index;
                return true;
            }

            if (!await _enumerator.MoveNext(cancellationToken))
                return false;

            _dict[++Index] = _enumerator.Current;
            return true;
        }

        public bool MovePrevious()
        {
            if (!_dict.ContainsKey(Index - 1))
                return false;

            --Index;
            return true;
        }

        public void Reset() => Index = -1;

        public void Dispose() => _enumerator.Dispose();
    }
}
