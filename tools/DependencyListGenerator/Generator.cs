// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.IO.Abstractions;
using DependencyListGenerator.DotNetOutdated.Services;

namespace DependencyListGenerator;

public static class Generator
{
    public static IEnumerable<TransientDependency> EnumerateDependencies(string projectPath, IReadOnlyCollection<string> excludedDependencies)
    {
        var dotNetRunner = new DotNetRunner();
        var fileSystem = new FileSystem();

        var analysisService = new ProjectAnalysisService(
            dependencyGraphService: new DependencyGraphService(dotNetRunner, fileSystem),
            dotNetRestoreService: new DotNetRestoreService(dotNetRunner, fileSystem),
            fileSystem: fileSystem);

        var result = analysisService.AnalyzeProject(projectPath, true, true, 1024)[0];
        var net462 = result.TargetFrameworks.First(x => x.Name.ToString() == ".NETFramework,Version=v4.6.2");

        foreach (var dep in net462.Dependencies.OrderBy(x => x.Name))
        {
            // OpenTelemetry and OpenTracing dependencies are managed directly.
            if (dep.IsTransitive && !dep.IsDevelopmentDependency && !dep.Name.StartsWith("OpenTelemetry") && dep.Name != "OpenTracing" && !excludedDependencies.Contains(dep.Name))
            {
                yield return new TransientDependency(dep.Name, dep.ResolvedVersion.ToString());
            }
        }
    }
}
