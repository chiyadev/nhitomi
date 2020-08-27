using System;
using System.IO;
using Elasticsearch.Net;
using Microsoft.IO;

namespace nhitomi.Database
{
    public class ElasticMemoryStreamFactory : IMemoryStreamFactory
    {
        readonly RecyclableMemoryStreamManager _manager;

        public ElasticMemoryStreamFactory(RecyclableMemoryStreamManager manager)
        {
            _manager = manager;
        }

        public MemoryStream Create() => _manager.GetStream();
        public MemoryStream Create(byte[] bytes) => _manager.GetStream(bytes);
        public MemoryStream Create(byte[] bytes, int index, int count) => _manager.GetStream(bytes.AsMemory().Slice(index, count));
    }
}