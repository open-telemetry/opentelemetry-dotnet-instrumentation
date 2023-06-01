// <copyright file="IntegrationToGenerate.cs" company="OpenTelemetry Authors">
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

namespace SourceGenerators;

internal class IntegrationToGenerate
{
    public string? IntegrationName { get; set; }

    public string? TargetAssembly { get; set; }

    public string? TargetType { get; set; }

    public string? TargetMethod { get; set; }

    public int TargetMinimumMajor { get; set; }

    public int TargetMinimumMinor { get; set; }

    public int TargetMinimumPatch { get; set; }

    public int TargetMaximumMajor { get; set; }

    public int TargetMaximumMinor { get; set; } = 65535;

    public int TargetMaximumPatch { get; set; } = 65535;

    public string? IntegrationType { get; set; }

    public string[]? TargetSignatureTypes { get; set; }
}
