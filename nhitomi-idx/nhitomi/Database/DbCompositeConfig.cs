using System;
using System.Collections.Generic;
using MessagePack;
using Nest;

namespace nhitomi.Database
{
    /// <summary>
    /// Contains server configuration.
    /// </summary>
    [MessagePackObject(true), ElasticsearchType(RelationName = nameof(Config))]
    public class DbCompositeConfig : IDbObject
    {
        public const string DefaultId = "config";

        public string Id
        {
            get => DefaultId;
            set { }
        }

        [Key("c"), Object(Name = "c", Enabled = false)]
        public Dictionary<string, string> Config { get; set; }

        public void UpdateCache(IServiceProvider services) { }
    }
}