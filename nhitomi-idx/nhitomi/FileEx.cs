using System.IO;

namespace nhitomi
{
    public static class FileEx
    {
        const int _bufferSize = 4096;

        /// <summary>
        /// Opens a <see cref="FileStream"/> capable of asynchronous operations.
        /// </summary>
        public static FileStream OpenAsync(string path, FileMode mode, FileAccess access = FileAccess.ReadWrite, FileShare share = FileShare.Read)
            => new FileStream(path, mode, access, share, _bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
    }
}