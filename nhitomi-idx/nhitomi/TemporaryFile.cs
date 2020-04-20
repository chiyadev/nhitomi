using System;
using System.IO;

namespace nhitomi
{
    public class TemporaryFile : IDisposable
    {
        public string Path { get; }

        public TemporaryFile()
        {
            Path = System.IO.Path.GetTempFileName();
        }

        public Stream OpenAsync(FileMode mode = FileMode.Open, FileAccess access = FileAccess.ReadWrite)
            => FileEx.OpenAsync(Path, mode, access);

        public void Dispose()
        {
            try
            {
                File.Delete(Path);
            }
            catch
            {
                // ignored
            }
        }
    }
}