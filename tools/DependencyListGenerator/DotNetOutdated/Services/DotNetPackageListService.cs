using System.IO.Abstractions;

namespace DependencyListGenerator.DotNetOutdated.Services;

public class DotNetPackageListService
{
    private readonly DotNetRunner _dotNetRunner;
    private readonly IFileSystem _fileSystem;

    public DotNetPackageListService(DotNetRunner dotNetRunner, IFileSystem fileSystem)
    {
        _dotNetRunner = dotNetRunner;
        _fileSystem = fileSystem;
    }

    public RunStatus ListPackages(string projectPath)
    {
        string[] arguments = new[]
        {
            "list", $"\"{projectPath}\"", "package", "--include-transitive", "--format", "json", "--output-version", "1"
        };

        return _dotNetRunner.Run(_fileSystem.Path.GetDirectoryName(projectPath), arguments);
    }
}
