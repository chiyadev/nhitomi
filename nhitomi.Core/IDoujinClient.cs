// Copyright (c) 2018-2019 fate/loli
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace nhitomi.Core
{
    public interface IDoujinClient : IDisposable
    {
        string Name { get; }
        string Url { get; }
        [JsonIgnore] string IconUrl { get; }

        double RequestThrottle { get; }

        DoujinClientMethod Method { get; }

        [JsonIgnore] Regex GalleryRegex { get; }

        Task<IDoujin> GetAsync(string id, CancellationToken cancellationToken = default);
        Task<IAsyncEnumerable<IDoujin>> SearchAsync(string query, CancellationToken cancellationToken = default);
    }
}
