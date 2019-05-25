// Copyright (c) 2018-2019 chiya.dev
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace nhitomi.Core
{
    public interface IDoujinClient : IDisposable
    {
        string Name { get; }

        string Url { get; }
        string IconUrl { get; }
        string GalleryRegex { get; }

        Task<Doujin> GetAsync(string id, CancellationToken cancellationToken = default);
        IAsyncEnumerable<string> EnumerateAsync(CancellationToken cancellationToken = default);
    }
}