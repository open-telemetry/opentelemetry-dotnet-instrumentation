namespace DependencyListGenerator.DotNetOutdated.Models;

public class TargetFramework(string name, IList<Dependency> dependencies)
{
    public IList<Dependency> Dependencies { get; } = dependencies;

    public string Name { get; set; } = name;
}
