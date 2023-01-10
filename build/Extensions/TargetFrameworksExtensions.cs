namespace Extensions;

public static class TargetFrameworksExtensions
{
    public static IEnumerable<TargetFramework> ExceptNetFramework(this IEnumerable<TargetFramework> frameworks)
    {
        return frameworks.Except(TargetFramework.NetFramework);
    }
}
