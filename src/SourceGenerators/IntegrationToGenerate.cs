// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace SourceGenerators;

internal readonly record struct IntegrationToGenerate(string IntegrationType, EquatableArray<TargetToGenerate> Targets)
{
    public readonly string IntegrationType = IntegrationType;
    public readonly EquatableArray<TargetToGenerate> Targets = Targets;
}
