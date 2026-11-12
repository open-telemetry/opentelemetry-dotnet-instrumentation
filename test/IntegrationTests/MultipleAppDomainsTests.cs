// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using IntegrationTests.Helpers;

namespace IntegrationTests;

public class MultipleAppDomainsTests : TestHelper
{
    public MultipleAppDomainsTests(ITestOutputHelper output)
        : base("MultipleAppDomains.NetFramework", output)
    {
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitsTraces()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        const int expectedSpanCount = 5;
        for (var i = 0; i < expectedSpanCount; i++)
        {
            collector.Expect("ByteCode.Plugin.StrongNamedValidation");
        }

        // Use the integrations file that bring the expected instrumentation.
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "ByteCode.Plugin.StrongNamedValidation");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_PLUGINS", "TestLibrary.InstrumentationTarget.Plugin, TestLibrary.InstrumentationTarget, Version=1.0.0.0, Culture=neutral, PublicKeyToken=c0db600a13f60b51");
        var (standardOutput, standardErrorOutput, _) = RunTestApplication();

        Assert.Contains("ModuleInitializerRan=True", standardOutput, StringComparison.Ordinal);
        // Nothing regarding log should have been logged to the console.
        Assert.DoesNotContain("Log:", standardErrorOutput, StringComparison.Ordinal);

        collector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SkipsLoaderInPartiallyTrustedAppDomain()
    {
        SetEnvironmentVariable("OTEL_TRACES_EXPORTER", "none");
        SetEnvironmentVariable("OTEL_METRICS_EXPORTER", "none");
        SetEnvironmentVariable("OTEL_LOGS_EXPORTER", "none");

        var (standardOutput, _, _) = RunTestApplication(new() { Arguments = "--partial-trust" });

        Assert.Contains("PartialTrustDomainIsFullyTrusted=False", standardOutput, StringComparison.Ordinal);
        Assert.Contains("PartialTrustDomainLoaderLoaded=False", standardOutput, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void UsesConfigurationFromEachAppDomain()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", null);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_PLUGINS", "TestLibrary.InstrumentationTarget.Plugin, TestLibrary.InstrumentationTarget, Version=1.0.0.0, Culture=neutral, PublicKeyToken=c0db600a13f60b51");

        collector.Expect("ByteCode.Plugin.StrongNamedValidation");
        collector.Expect("ByteCode.Plugin.StrongNamedValidation");
        collector.ExpectCollected(spans =>
        {
            var resources = spans.Select(span => span.Resource.Attributes.ToDictionary(attribute => attribute.Key, attribute => attribute.Value.StringValue)).ToList();
            return resources.Any(resource => resource["service.name"] == "domain-1" && resource["domain.number"] == "1")
                && resources.Any(resource => resource["service.name"] == "domain-2" && resource["domain.number"] == "2");
        });

        RunTestApplication(new() { Arguments = "--configured-app-domains" });

        collector.AssertExpectations();
    }
}
#endif
