namespace Extensions;

public static class TargetFrameworksExtensions
{
    public static IEnumerable<TargetFramework> ExceptNetFramework(this IEnumerable<TargetFramework> frameworks)
    {
        return frameworks.Except(TargetFramework.NetFramework);
    }

    public static Version GetVersion(this TargetFramework framework)
    {
        if (framework == TargetFramework.NET462)
        {
            return new Version(4, 6, 2);
        }
        if (framework == TargetFramework.NET47)
        {
            return new Version(4, 7, 0);
        }
        if (framework == TargetFramework.NET471)
        {
            return new Version(4, 7, 1);
        }
        if (framework == TargetFramework.NET472)
        {
            return new Version(4, 7, 2);
        }

        throw new ArgumentOutOfRangeException(nameof(framework));
    }
}
