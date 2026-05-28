using System.IO.Abstractions;

namespace DependencyListGenerator.DotNetOutdated.Services;

public class DotNetRestoreService(IFileSystem fileSystem)
{
    public RunStatus Restore(string projectPath)
    {
        string[] arguments = ["restore", $"\"{projectPath}\""];

        return DotNetRunner.Run(fileSystem.Path.GetDirectoryName(projectPath), arguments);
    }
}
