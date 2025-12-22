// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

public class OpenTracingTests : TestHelper
{
    public OpenTracingTests(ITestOutputHelper output)
        : base("OpenTracing", output)
    {
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task SubmitsTraces()
    {
        using var collector = await MockSpansCollector.InitializeAsync(Output);
        SetExporter(collector);
        collector.Expect("opentracing-shim");

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPENTRACING_ENABLED", "true");
        RunTestApplication();

        collector.AssertExpectations();
    }
}
