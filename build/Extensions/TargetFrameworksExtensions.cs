namespace Extensions;

public static class TargetFrameworksExtensions
{
    public static IEnumerable<TargetFramework> ExceptNetFramework(this IEnumerable<TargetFramework> frameworks)
    {
        return frameworks.Except(TargetFramework.NetFramework);
    }

    public static IEnumerable<TargetFramework> ExceptNet(this IEnumerable<TargetFramework> frameworks)
    {
        return frameworks.Except(TargetFramework.Net);
    }
}
