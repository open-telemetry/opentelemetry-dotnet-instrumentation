using System.IO.Abstractions;
using System.Text.Json;
using DependencyListGenerator.DotNetOutdated.Models;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Versioning;

namespace DependencyListGenerator.DotNetOutdated.Services;

public class ProjectAnalysisService
{
    private readonly DotNetRestoreService _dotNetRestoreService;
    private readonly DotNetPackageListService _packageListService;
    private readonly IFileSystem _fileSystem;

    public ProjectAnalysisService(DotNetRestoreService dotNetRestoreService, DotNetPackageListService packageListService, IFileSystem fileSystem)
    {
        _dotNetRestoreService = dotNetRestoreService;
        _packageListService = packageListService;
        _fileSystem = fileSystem;
    }

    public List<Project> AnalyzeProject(string projectPath, bool runRestore)
    {
        if (runRestore)
        {
            _dotNetRestoreService.Restore(projectPath);
        }

        var packagesFolder =
            SettingsUtility.GetGlobalPackagesFolder(
                Settings.LoadDefaultSettings(root: _fileSystem.Path.GetDirectoryName(projectPath)));

        var result = _packageListService.ListPackages(projectPath);
        if (!result.IsSuccess)
        {
            return null;
        }

        var packageListModel = JsonSerializer.Deserialize<PackageListModel>(result.Output);

        var projects = new List<Project>();
        foreach (var modelProject in packageListModel.Projects)
        {
            var targets = new List<TargetFramework>();
            // Get the target frameworks with their dependencies
            foreach (var targetFrameworkInformation in modelProject.Frameworks)
            {
                var dependencies = new List<Dependency>();
                foreach (var projectDependency in targetFrameworkInformation.TopLevelPackages.Concat(
                             targetFrameworkInformation.TransitivePackages))
                {
                    // Determine whether this is a development dependency
                    var isDevelopmentDependency = false;
                    var packageIdentity = new PackageIdentity(projectDependency.Id, NuGetVersion.Parse(projectDependency.ResolvedVersion));
                    var packageInfo =
                        LocalFolderUtility.GetPackageV3(packagesFolder, packageIdentity, NullLogger.Instance);
                    if (packageInfo != null)
                    {
                        isDevelopmentDependency = packageInfo.GetReader().GetDevelopmentDependency();
                    }

                    dependencies.Add(new Dependency(projectDependency.Id, projectDependency.ResolvedVersion, isDevelopmentDependency));
                }

                targets.Add(new TargetFramework(targetFrameworkInformation.Framework, dependencies));
            }

            projects.Add(new Project(modelProject.Path, targets));
        }

        return projects;
    }
}
