// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.IO.Abstractions;
using DependencyListGenerator.DotNetOutdated.Services;

namespace DependencyListGenerator;

public static class Generator
{
    public static Dictionary<string, TransientDependency[]> EnumerateDependencies(string projectPath)
    {
        var fileSystem = new FileSystem();

        var analysisService = new ProjectAnalysisService(
            dotNetRestoreService: new DotNetRestoreService(fileSystem),
            packageListService: new DotNetPackageListService(fileSystem),
            fileSystem: fileSystem);

        var result = analysisService.AnalyzeProject(projectPath, runRestore: true).Single();
        return result.TargetFrameworks.ToDictionary(
            it => it.Name,
            it => it.Dependencies.OrderBy(x => x.Name)
                .Where(dep => !dep.IsDevelopmentDependency)
                .Select(dep => new TransientDependency(dep.Name, dep.ResolvedVersion)).ToArray());
    }
}
