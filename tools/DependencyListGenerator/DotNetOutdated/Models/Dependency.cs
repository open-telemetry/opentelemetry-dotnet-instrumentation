// <copyright file="Dependency.cs" company="OpenTelemetry Authors">
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

public class Dependency
{
    public Dependency(string name, VersionRange versionRange, NuGetVersion resolvedVersion, bool isAutoReferenced, bool isTransitive, bool isDevelopmentDependency, bool isVersionCentrallyManaged)
    {
        Name = name;
        VersionRange = versionRange;
        ResolvedVersion = resolvedVersion;
        IsAutoReferenced = isAutoReferenced;
        IsTransitive = isTransitive;
        IsDevelopmentDependency = isDevelopmentDependency;
        IsVersionCentrallyManaged = isVersionCentrallyManaged;
    }

    public bool IsAutoReferenced { get; }

    public bool IsDevelopmentDependency { get; }

    public bool IsTransitive { get; }

    public bool IsVersionCentrallyManaged { get; }

    public string Name { get; }

    public NuGetVersion ResolvedVersion { get; }

    public VersionRange VersionRange { get; }
}
