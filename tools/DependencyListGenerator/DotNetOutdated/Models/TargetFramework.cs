using NuGet.Frameworks;

namespace DependencyListGenerator.DotNetOutdated.Models;

public class TargetFramework
{
    public TargetFramework(NuGetFramework name)
    {
        Name = name;
    }

    public IList<Dependency> Dependencies { get; } = new List<Dependency>();

    public NuGetFramework Name { get; set; }
}
