// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;

namespace IntegrationTests;

public class LegacySourcesTests : TestHelper
{
    private const string ServiceName = "TestApplication.TracesLegacySource";

    // must match TestApplication.TracesLegacySource.Program.LegacySourceName
    private const string LegacySourceName = "ManualSpan";

    public LegacySourcesTests(ITestOutputHelper output)
        : base("TracesLegacySource", output)
    {
        SetEnvironmentVariable("OTEL_SERVICE_NAME", ServiceName);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitsLegacyActivityWhenSourceIsRegistered()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        // legacy activities have no ActivitySource, so they export under an empty scope name
        collector.Expect(string.Empty, span => span.Name == LegacySourceName);

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_LEGACY_SOURCES", LegacySourceName);
        RunTestApplication();

        collector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void DoesNotSubmitLegacyActivityWhenSourceIsNotRegistered()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        // without the env var the legacy source is not registered, so nothing is collected
        RunTestApplication();

        collector.AssertEmpty();
    }
}
