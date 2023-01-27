// <copyright file="DotNetRestoreService.cs" company="OpenTelemetry Authors">
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
