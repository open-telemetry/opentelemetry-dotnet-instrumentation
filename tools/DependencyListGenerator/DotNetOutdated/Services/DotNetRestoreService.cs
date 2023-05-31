using System.IO.Abstractions;

namespace DependencyListGenerator.DotNetOutdated.Services;

public class DotNetRestoreService
{
    private readonly DotNetRunner _dotNetRunner;
    private readonly IFileSystem _fileSystem;

    public DotNetRestoreService(DotNetRunner dotNetRunner, IFileSystem fileSystem)
    {
        _dotNetRunner = dotNetRunner;
        _fileSystem = fileSystem;
    }

    public RunStatus Restore(string projectPath)
    {
        string[] arguments = new[] { "restore", $"\"{projectPath}\"" };

        return _dotNetRunner.Run(_fileSystem.Path.GetDirectoryName(projectPath), arguments);
    }
}
