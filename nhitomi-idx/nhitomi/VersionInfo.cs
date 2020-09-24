using System;

namespace nhitomi
{
    public static class VersionInfo
    {
        /// <summary>
        /// Git commit hash of the current version.
        /// </summary>
        public static string Version { get; } = Environment.GetEnvironmentVariable("ASPNETCORE_APP_VERSION") ?? "Latest";
    }
}