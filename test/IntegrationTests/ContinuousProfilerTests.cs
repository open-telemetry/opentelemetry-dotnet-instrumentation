// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET6_0_OR_GREATER

using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

public class ContinuousProfilerTests : TestHelper
{
    public ContinuousProfilerTests(ITestOutputHelper output)
        : base("ContinuousProfiler", output)
    {
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void ProfilerTestApplicationExecutesWithoutErrors()
    {
        RunTestApplication();
    }
}
#endif
