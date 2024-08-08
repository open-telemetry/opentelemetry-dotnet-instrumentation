// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace SourceGenerators;

internal readonly record struct TargetToGenerate(int SignalType, string IntegrationName, string Assembly, string Type, string Method, int MinimumMajor, int MinimumMinor, int MinimumPatch, int MaximumMajor, int MaximumMinor, int MaximumPatch, string SignatureTypes, int IntegrationKind)
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
    public readonly int IntegrationKind = IntegrationKind;
}
