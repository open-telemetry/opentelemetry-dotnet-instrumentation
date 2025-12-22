// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using System.Reflection;
#endif
using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

public class StrongNamedTests : TestHelper
{
    public StrongNamedTests(ITestOutputHelper output)
        : base("StrongNamed", output)
    {
    }

    [Fact]
    public async Task SubmitsTraces()
    {
        using var collector = await MockSpansCollector.InitializeAsync(Output);
        SetExporter(collector);
        collector.Expect("ByteCode.Plugin.StrongNamedValidation");

        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "ByteCode.Plugin.StrongNamedValidation");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_PLUGINS", "TestLibrary.InstrumentationTarget.Plugin, TestLibrary.InstrumentationTarget, Version=1.0.0.0, Culture=neutral, PublicKeyToken=c0db600a13f60b51");
        RunTestApplication();

        // TODO: When native logs are moved to an EventSource implementation check for the log
        // TODO: entries reporting the missing instrumentation type and missing instrumentation methods.
        // TODO: See https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/960

        collector.AssertExpectations();
    }

#if NETFRAMEWORK
    [Fact]
    public void VerifyIfApplicationHasStrongName()
    {
        var testApplicationPath = EnvironmentHelper.GetTestApplicationPath();

        var assembly = Assembly.ReflectionOnlyLoadFrom(testApplicationPath);

        Assert.Equal("c0db600a13f60b51", BitConverter.ToString(assembly.GetName().GetPublicKeyToken()).Replace("-", string.Empty).ToLowerInvariant());
    }
#endif

}
