using System.IO.Abstractions;

namespace DependencyListGenerator.DotNetOutdated.Services;

public class DotNetRestoreService
{
    private readonly IFileSystem _fileSystem;

    public DotNetRestoreService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public RunStatus Restore(string projectPath)
    {
        string[] arguments = ["restore", $"\"{projectPath}\""];

        return DotNetRunner.Run(_fileSystem.Path.GetDirectoryName(projectPath), arguments);
    }
}
