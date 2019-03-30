using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace nhitomi
{
    public class ProxyList : List<ProxyInfo>
    {
        public readonly object Lock = new object();

        public TimeSpan ProxyLifetime { get; set; } = TimeSpan.FromMinutes(1);

        public IEnumerable<ProxyInfo> ActiveProxies =>
            this.Where(p => p.RegisterTime + ProxyLifetime >= DateTime.UtcNow);
    }

    public class ProxyInfo
    {
        [JsonProperty("u")] public string Url;
        [JsonProperty("r")] public DateTime RegisterTime;
    }
}