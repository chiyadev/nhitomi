// Copyright (c) 2018-2019 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.IO;
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
        string IconUrl { get; }

        DoujinClientMethod Method { get; }

        [JsonIgnore] Regex GalleryRegex { get; }

        Task<IDoujin> GetAsync(string id);
        Task<IAsyncEnumerable<IDoujin>> SearchAsync(string query);

        Task UpdateAsync();

        double RequestThrottle { get; }
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

            public DoujinClientMethod Method => _impl.Method;

            public Regex GalleryRegex => _impl.GalleryRegex;

            public async Task<IDoujin> GetAsync(string id)
            {
                await _semaphore.WaitAsync();
                try
                {
                    var doujin = await _impl.GetAsync(id);

                    await Task.Delay(TimeSpan.FromMilliseconds(RequestThrottle));

                    return doujin;
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            public async Task<IAsyncEnumerable<IDoujin>> SearchAsync(string query)
            {
                await _semaphore.WaitAsync();
                try
                {
                    var results = await _impl.SearchAsync(query);

                    await Task.Delay(TimeSpan.FromMilliseconds(RequestThrottle));

                    return results;
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            public async Task UpdateAsync()
            {
                await _semaphore.WaitAsync();
                try
                {
                    await _impl.UpdateAsync();

                    await Task.Delay(TimeSpan.FromMilliseconds(RequestThrottle));
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            public double RequestThrottle => _impl.RequestThrottle;

            public void Dispose() => _semaphore.Dispose();
        }

        public static IDoujinClient Synchronized(this IDoujinClient client) => new SynchronizedClient(client);
    }
}