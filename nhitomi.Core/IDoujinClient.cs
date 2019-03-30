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

    public enum DoujinClientMethod
    {
        Html,
        Api
    }

    public static class DoujinClientExtensions
    {
        sealed class SynchronizedClient : IDoujinClient
        {
            readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
            readonly IDoujinClient _impl;

            public SynchronizedClient(IDoujinClient impl)
            {
                _impl = impl;
            }

            public string Name => _impl.Name;
            public string Url => _impl.Url;
            public string IconUrl => _impl.IconUrl;

            public double RequestThrottle => _impl.RequestThrottle;

            public DoujinClientMethod Method => _impl.Method;

            public Regex GalleryRegex => _impl.GalleryRegex;

            public async Task<IDoujin> GetAsync(string id, CancellationToken cancellationToken = default)
            {
                await _semaphore.WaitAsync(cancellationToken);
                try
                {
                    return await _impl.GetAsync(id, cancellationToken);
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            public async Task<IAsyncEnumerable<IDoujin>> SearchAsync(
                string query,
                CancellationToken cancellationToken = default)
            {
                await _semaphore.WaitAsync(cancellationToken);
                try
                {
                    return await _impl.SearchAsync(query, cancellationToken);
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            public void Dispose() => _semaphore.Dispose();
        }

        public static IDoujinClient Synchronized(this IDoujinClient client) => new SynchronizedClient(client);
    }
}