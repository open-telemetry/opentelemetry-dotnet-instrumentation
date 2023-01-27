// <copyright file="DependencyGraphService.cs" company="OpenTelemetry Authors">
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
using DependencyListGenerator.DotNetOutdated.Exceptions;
using NuGet.ProjectModel;

namespace DependencyListGenerator.DotNetOutdated.Services;

/// <remarks>
/// Credit for the stuff happening in here goes to the https://github.com/jaredcnance/dotnet-status project
/// </remarks>
public class DependencyGraphService
{
    private readonly DotNetRunner _dotNetRunner;
    private readonly IFileSystem _fileSystem;

    public DependencyGraphService(DotNetRunner dotNetRunner, IFileSystem fileSystem)
    {
        _dotNetRunner = dotNetRunner;
        _fileSystem = fileSystem;
    }

    public DependencyGraphSpec GenerateDependencyGraph(string projectPath)
    {
        var dgOutput = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), _fileSystem.Path.GetTempFileName());

        string[] arguments = { "msbuild", $"\"{projectPath}\"", "/t:Restore,GenerateRestoreGraphFile", $"/p:RestoreGraphOutputPath=\"{dgOutput}\"" };

        var runStatus = _dotNetRunner.Run(_fileSystem.Path.GetDirectoryName(projectPath), arguments);

        if (runStatus.IsSuccess)
        {
            /*
                TempDirectory is a hacky workaround for DependencyGraphSpec(JObject)
                being deprecated. Unfortunately it looks like the only alternative
                is to load the file locally. Which is ok normally, but complicates
                testing.
            */
            using (var tempDirectory = new TempDirectory())
            {
                var dependencyGraphFilename = Path.Combine(tempDirectory.DirectoryPath, "DependencyGraph.json");
                var dependencyGraphText = _fileSystem.File.ReadAllText(dgOutput);
                File.WriteAllText(dependencyGraphFilename, dependencyGraphText);
                return DependencyGraphSpec.Load(dependencyGraphFilename);
            }
        }

        throw new CommandValidationException($"Unable to process the project `{projectPath}. Are you sure this is a valid .NET Core or .NET Standard project type?" +
                                             $"{Environment.NewLine}{Environment.NewLine}Here is the full error message returned from the Microsoft Build Engine:{Environment.NewLine}{Environment.NewLine}{runStatus.Output} - {runStatus.Errors} - exit code: {runStatus.ExitCode}");
    }
}
