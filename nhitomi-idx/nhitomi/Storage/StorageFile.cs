using System;
using System.IO;
using System.Threading.Tasks;

namespace nhitomi.Storage
{
    public class StorageFile : IAsyncDisposable
    {
        public string Name { get; set; }
        public Stream Stream { get; set; }

        string _mediaType;

        public string MediaType
        {
            get => string.IsNullOrEmpty(_mediaType) ? "application/octet-stream" : _mediaType;
            set => _mediaType = value;
        }

        public StorageFile() { }

        public StorageFile(string name, Stream stream, string mediaType)
        {
            Name      = name;
            Stream    = stream;
            MediaType = mediaType;
        }

        public ValueTask DisposeAsync()
        {
            if (Stream == null)
                return default;

            return Stream.DisposeAsync();
        }
    }
}