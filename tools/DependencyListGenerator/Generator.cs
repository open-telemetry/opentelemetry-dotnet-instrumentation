// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.IO.Abstractions;
using DependencyListGenerator.DotNetOutdated.Services;

namespace DependencyListGenerator;

public static class Generator
{
    public static Dictionary<string, TransientDependency[]> EnumerateDependencies(string projectPath)
    {
        var dotNetRunner = new DotNetRunner();
        var fileSystem = new FileSystem();

        var analysisService = new ProjectAnalysisService(
            dotNetRestoreService: new DotNetRestoreService(dotNetRunner, fileSystem),
            packageListService: new DotNetPackageListService(dotNetRunner, fileSystem),
            fileSystem: fileSystem);

        var result = analysisService.AnalyzeProject(projectPath, true)[0];
        return result.TargetFrameworks.ToDictionary(
            it => it.Name,
            it => it.Dependencies.OrderBy(x => x.Name)
                .Where(dep => !dep.IsDevelopmentDependency)
                .Select(dep => new TransientDependency(dep.Name, dep.ResolvedVersion)).ToArray());
    }
}
