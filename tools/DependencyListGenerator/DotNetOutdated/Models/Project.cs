// <copyright file="Project.cs" company="OpenTelemetry Authors">
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

using NuGet.Versioning;

namespace DependencyListGenerator.DotNetOutdated.Models;

public class Project
{
    public Project(string name, string filePath, IEnumerable<Uri> sources, NuGetVersion version)
    {
        FilePath = filePath;
        Name = name;
        Sources = new List<Uri>(sources);
        Version = version;
    }

    public string FilePath { get; }

    public string Name { get; }

    public IList<Uri> Sources { get; }

    public IList<TargetFramework> TargetFrameworks { get; } = new List<TargetFramework>();

    public NuGetVersion Version { get; }
}
