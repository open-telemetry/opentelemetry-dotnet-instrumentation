using System.IO.Abstractions;

namespace DependencyListGenerator.DotNetOutdated.Services;

public class DotNetPackageListService(IFileSystem fileSystem)
{
    public RunStatus ListPackages(string projectPath)
    {
        string[] arguments =
        [
            "list", $"\"{projectPath}\"", "package", "--include-transitive", "--format", "json", "--output-version", "1"
        ];

        return DotNetRunner.Run(fileSystem.Path.GetDirectoryName(projectPath), arguments);
    }
}
