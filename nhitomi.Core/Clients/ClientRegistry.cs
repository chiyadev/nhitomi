using System;
using System.Collections.Generic;
using System.Linq;

namespace nhitomi.Core.Clients
{
    public static class ClientRegistry
    {
        static readonly IReadOnlyDictionary<string, Type> _clientFactory = typeof(IDoujinClient).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && t.IsClass && typeof(IDoujinClient).IsAssignableFrom(t))
            .ToDictionary(t => t.Name.SubstringFromEnd("Client".Length), t => t);

        public static string FixSource(string source)
        {
            foreach (var clientName in _clientFactory.Keys)
                if (source.Equals(clientName, StringComparison.OrdinalIgnoreCase))
                    return clientName;

            return source;
        }

        // fixes trailing zeros if ID is an integer
        public static string FixSourceId(string id) => long.TryParse(id, out var longId) ? longId.ToString() : id;
    }
}