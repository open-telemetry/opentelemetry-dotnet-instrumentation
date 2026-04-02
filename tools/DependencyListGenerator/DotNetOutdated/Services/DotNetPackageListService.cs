using System.IO.Abstractions;

namespace DependencyListGenerator.DotNetOutdated.Services;

public class DotNetPackageListService
{
    private readonly IFileSystem _fileSystem;

    public DotNetPackageListService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public RunStatus ListPackages(string projectPath)
    {
        string[] arguments =
        [
            "list", $"\"{projectPath}\"", "package", "--include-transitive", "--format", "json", "--output-version", "1"
        ];

        return DotNetRunner.Run(_fileSystem.Path.GetDirectoryName(projectPath), arguments);
    }
}
