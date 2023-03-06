// <copyright file="Generator.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System.IO.Abstractions;
using DependencyListGenerator.DotNetOutdated.Services;

namespace DependencyListGenerator;

public static class Generator
{
    public static IEnumerable<TransientDependency> EnumerateDependencies(string projectPath)
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
            if (dep.IsTransitive && !dep.IsDevelopmentDependency && !dep.Name.StartsWith("OpenTelemetry") && dep.Name != "OpenTracing")
            {
                yield return new TransientDependency(dep.Name, dep.ResolvedVersion.ToString());
            }
        }
    }
}
