// <copyright file="TargetToGenerate.cs" company="OpenTelemetry Authors">
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

internal readonly record struct TargetToGenerate(int SignalType, string IntegrationName, string Assembly, string Type, string Method, int MinimumMajor, int MinimumMinor, int MinimumPatch, int MaximumMajor, int MaximumMinor, int MaximumPatch, string SignatureTypes)
{
    public readonly int SignalType = SignalType;

    public readonly string IntegrationName = IntegrationName;

    public readonly string Assembly = Assembly;

    public readonly string Type = Type;

    public readonly string Method = Method;

    public readonly int MinimumMajor = MinimumMajor;

    public readonly int MinimumMinor = MinimumMinor;

    public readonly int MinimumPatch = MinimumPatch;

    public readonly int MaximumMajor = MaximumMajor;

    public readonly int MaximumMinor = MaximumMinor;

    public readonly int MaximumPatch = MaximumPatch;

    public readonly string SignatureTypes = SignatureTypes;
}
