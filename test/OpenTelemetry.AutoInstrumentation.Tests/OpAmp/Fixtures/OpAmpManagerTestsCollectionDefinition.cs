// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Tests.OpAmp.Fixtures;

#pragma warning disable CA1515 // Consider making public types internal. Required for xUnit discovery.
[CollectionDefinition(Name)]
public sealed class OpAmpManagerTestsCollectionDefinition
{
    public const string Name = "OpAmp manager tests";
}
#pragma warning restore CA1515 // Consider making public types internal. Required for xUnit discovery.
