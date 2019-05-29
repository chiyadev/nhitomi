using System;

namespace nhitomi.Core.Clients.Testing
{
    public abstract class ClientTestCase
    {
        public abstract string DoujinId { get; }
        public abstract Type ClientType { get; }

        public abstract DoujinInfo KnownValue { get; }
    }
}