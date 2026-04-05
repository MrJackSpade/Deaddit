using System.Reflection;

namespace Deaddit
{
    internal static class VersionInfo
    {
        public static string Version { get; } =
            typeof(VersionInfo).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "dev";
    }
}
