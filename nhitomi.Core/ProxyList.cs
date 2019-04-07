using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace nhitomi.Core
{
    public class ProxyList : List<ProxyInfo>
    {
        public readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1);
    }

    public class ProxyInfo
    {
        public string Url;
        public IPAddress IPAddress;
        public string RegistrationToken;
    }
}
