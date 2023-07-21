using System.Reflection;

public static class VersionHelper
{
    public static string GetVersion()
    {
        return typeof(VersionHelper).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];
    }
}
