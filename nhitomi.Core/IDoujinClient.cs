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

        /// <summary>
        /// Retrieves doujin information by its identifier asynchronously.
        /// </summary>
        Task<DoujinInfo> GetAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an asynchronous enumerable that enumerates chronologically from the specified identifier.
        /// </summary>
        IAsyncEnumerable<string> EnumerateAsync(string id = null);
    }
}