using System;

namespace nhitomi
{
    public static class VersionInfo
    {
        /// <summary>
        /// Git commit hash of the current version.
        /// </summary>
        public static string Version { get; } = "Latest";

        static VersionInfo()
        {
            var version = Environment.GetEnvironmentVariable("ASPNETCORE_APP_VERSION");

            if (!string.IsNullOrEmpty(version))
                Version = version;
        }
    }
}