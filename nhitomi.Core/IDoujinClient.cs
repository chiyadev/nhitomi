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
        DoujinClientInfo Info { get; }

        string GalleryRegex { get; }

        Task<DoujinInfo> GetAsync(string id, CancellationToken cancellationToken = default);
        Task<IEnumerable<string>> EnumerateAsync(string startId = null, CancellationToken cancellationToken = default);
    }
}