using System.Reflection;

public static class VersionHelper
{
    public static string GetVersion()
    {
        return typeof(VersionHelper).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];
    }

    public static string GetVersionWithoutSuffixes()
    {
        return GetVersion().Split('-')[0];
    }
}
