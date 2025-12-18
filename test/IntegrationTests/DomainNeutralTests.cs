// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

public class DomainNeutralTests : TestHelper
{
    public DomainNeutralTests(ITestOutputHelper output)
        : base("DomainNeutral.NetFramework", output)
    {
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task SubmitsTraces()
    {
        Assert.True(EnvironmentTools.IsWindowsAdministrator(), "This test requires Windows Administrator privileges.");

        using var collector = await MockSpansCollector.InitializeAsync(Output);
        SetExporter(collector);
        collector.Expect("ByteCode.Plugin.StrongNamedValidation");

        // Add the necessary assembly to the GAC so it can be loaded as domain-neutral.
        var instrumentationAssembly = Path.Combine(
            EnvironmentTools.GetSolutionDirectory(),
            "bin",
            "tracer-home",
            "netfx",
            "OpenTelemetry.AutoInstrumentation.dll");
        Assert.True(File.Exists(instrumentationAssembly), $"instrumentation assembly is not available {instrumentationAssembly}");
        using var gacEntry = new GacEntry(instrumentationAssembly);

        // Domain-neutral depends on strong named assemblies to work, leverage some assets from
        // strong name testing in the current test.
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "ByteCode.Plugin.StrongNamedValidation");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_PLUGINS", "TestLibrary.InstrumentationTarget.Plugin, TestLibrary.InstrumentationTarget, Version=1.0.0.0, Culture=neutral, PublicKeyToken=c0db600a13f60b51");
        RunTestApplication();

        collector.AssertExpectations();
    }
}
#endif
