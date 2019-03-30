using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;

namespace nhitomi.Core
{
    public class ProxyList : List<ProxyInfo>
    {
        public readonly object Lock = new object();

        public TimeSpan ProxyLifetime { get; set; } = TimeSpan.FromMinutes(1);

        public void Update() => RemoveAll(p => p.RegisterTime + ProxyLifetime < DateTime.UtcNow);
    }

    public class ProxyInfo
    {
        [JsonProperty("u")] public string Url;
        [JsonProperty("r")] public DateTime RegisterTime;
        [JsonProperty("ip")] public IPAddress IPAddress;
    }
}