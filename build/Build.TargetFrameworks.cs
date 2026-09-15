using Extensions;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    // TODO NET11TODO revert this temporary workaround when NuGet packages compatible with the .NET 11 SDK are available.
    private readonly Dictionary<string, string[]> _targetFrameworksByProject =
        new(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

    private IReadOnlyCollection<string> GetProjectTargetFrameworks(Project project)
    {
        ResolveProjectTargetFrameworks([project]);
        return _targetFrameworksByProject[NormalizeProjectPath(project.Path)];
    }

    private void ResolveProjectTargetFrameworks(IEnumerable<Project> projects)
    {
        var unresolvedProjects = projects
            .Where(project => !_targetFrameworksByProject.ContainsKey(NormalizeProjectPath(project.Path)))
            .DistinctBy(project => NormalizeProjectPath(project.Path))
            .ToArray();

        if (unresolvedProjects.Length == 0)
        {
            return;
        }

        TemporaryDirectory.CreateDirectory();
        var identifier = Guid.NewGuid().ToString("N");
        var projectsFile = TemporaryDirectory / $"target-framework-projects-{identifier}.txt";
        var outputFile = TemporaryDirectory / $"target-frameworks-{identifier}.txt";

        try
        {
            File.WriteAllLines(projectsFile, unresolvedProjects.Select(project => project.Path.ToString()));

            DotNetMSBuild(s => s
                .SetTargetPath(RootDirectory / "build" / "GetTargetFrameworks.proj")
                .SetTargets("CollectTargetFrameworks")
                .SetConfiguration(BuildConfiguration)
                .SetTargetPlatform(Platform)
                .SetProperty("ProjectsFile", projectsFile)
                .SetProperty("OutputFile", outputFile)
                .SetMaxCpuCount(Environment.ProcessorCount)
                .SetNodeReuse(false)
                .SetNoLogo(true));

            foreach (var line in File.ReadLines(outputFile))
            {
                var separatorIndex = line.IndexOf('|');
                if (separatorIndex < 1)
                {
                    throw new InvalidOperationException($"Unexpected target framework output: '{line}'.");
                }

                var projectPath = NormalizeProjectPath(line[..separatorIndex]);
                var targetFrameworks = line[(separatorIndex + 1)..]
                    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                _targetFrameworksByProject[projectPath] = targetFrameworks;
            }

            foreach (var project in unresolvedProjects)
            {
                var projectPath = NormalizeProjectPath(project.Path);
                if (!_targetFrameworksByProject.TryGetValue(projectPath, out var targetFrameworks) || targetFrameworks.Length == 0)
                {
                    throw new InvalidOperationException($"Could not determine target frameworks for project '{project.Path}'.");
                }
            }
        }
        finally
        {
            projectsFile.DeleteFile();
            outputFile.DeleteFile();
        }
    }

    private static string NormalizeProjectPath(string projectPath) => Path.GetFullPath(projectPath);
}
