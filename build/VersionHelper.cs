using System.Reflection;

public static class VersionHelper
{
    static Lazy<string> Version = new Lazy<string>(() =>
        typeof(VersionHelper).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion);

    public static string GetVersion()
    {
        return Version.Value.Split('+')[0];
    }

    public static string GetCommitId()
    {
        return Version.Value.Split('+')[1];
    }

    public static string GetVersionWithoutSuffixes()
    {
        return Version.Value.Split('-', '+')[0];
    }

    public static (string Major, string Minor, string Patch) GetVersionParts()
    {
        var split = GetVersionWithoutSuffixes().Split(".");
        return (split[0], split[1], split[2]);
    }
}
