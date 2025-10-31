using NuGet.Versioning;

namespace DependencyListGenerator.DotNetOutdated.Models;

public class Project(string filePath, IList<TargetFramework> targets)
{
    public string FilePath { get; } = filePath;

    public IList<TargetFramework> TargetFrameworks { get; } = targets;
}
