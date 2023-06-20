using NuGet.Versioning;

namespace DependencyListGenerator.DotNetOutdated.Models;

public class Project
{
    public Project(string name, string filePath, IEnumerable<Uri> sources, NuGetVersion version)
    {
        FilePath = filePath;
        Name = name;
        Sources = new List<Uri>(sources);
        Version = version;
    }

    public string FilePath { get; }

    public string Name { get; }

    public IList<Uri> Sources { get; }

    public IList<TargetFramework> TargetFrameworks { get; } = new List<TargetFramework>();

    public NuGetVersion Version { get; }
}
