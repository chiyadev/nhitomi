// Copyright (c) 2018-2019 phosphene47
//
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace nhitomi.Core
{
    public static class Hitomi2
    {
    }

    /// <summary>
    /// Refer to https://namu.wiki/w/Hitomi.la#s-4.4.1 (Korean)
    /// </summary>
    public class HitomiClient2 : IDoujinClient
    {
        public string Name => nameof(Hitomi);
        public string Url => "https://hitomi.la/";
        public string IconUrl => "https://ltn.hitomi.la/favicon-160x160.png";

        public DoujinClientMethod Method => DoujinClientMethod.Api;

        public Regex GalleryRegex { get; } =
            new Regex(Hitomi.GalleryRegex, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        readonly PhysicalCache _cache;
        readonly HttpClient _http;
        readonly JsonSerializer _json;
        readonly ILogger _logger;

        public HitomiClient2(
            IHttpClientFactory httpFactory,
            JsonSerializer json,
            ILogger<HitomiClient2> logger
        )
        {
            _http = httpFactory?.CreateClient(Name);
            _cache = new PhysicalCache(Name, json);
            _json = json;
            _logger = logger;
        }

        public Task<IDoujin> GetAsync(string id) => throw new System.NotImplementedException();

        public Task<IAsyncEnumerable<IDoujin>> SearchAsync(string query) => throw new System.NotImplementedException();

        public Task UpdateAsync() => throw new System.NotImplementedException();

        public double RequestThrottle => Hitomi.RequestCooldown;

        public void Dispose()
        {
        }
    }
}
