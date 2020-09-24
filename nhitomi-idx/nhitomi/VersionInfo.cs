using System.IO;

namespace nhitomi
{
    public static class VersionInfo
    {
        /// <summary>
        /// Git commit hash of the current version.
        /// </summary>
        public static string Commit { get; } = "Unknown";

        static VersionInfo()
        {
            try
            {
                Commit = File.ReadAllText("version.txt");
            }
            catch (IOException) { }
        }
    }
}