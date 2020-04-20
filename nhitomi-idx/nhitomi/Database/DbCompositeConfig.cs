using MessagePack;
using Nest;
using nhitomi.Models;

namespace nhitomi.Database
{
    /// <summary>
    /// Represents server configuration.
    /// </summary>
    [MessagePackObject(true), ElasticsearchType(RelationName = nameof(CompositeConfig))]
    public class DbCompositeConfig : CompositeConfig, IDbObject
    {
        public const string DefaultId = "config";

        public string Id
        {
            get => DefaultId;
            set { }
        }

        public void UpdateCache() { }
    }
}