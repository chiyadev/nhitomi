using System;
using System.Reflection;

namespace nhitomi
{
    public static class VersionHelper
    {
        static Assembly Assembly => typeof(Startup).Assembly;

        public static Version Version => Assembly.GetName()
                                                 .Version;

        public static string Codename => Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                                 .InformationalVersion;
    }
}