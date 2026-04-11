// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

public class AssemblyRedirectionTests(ITestOutputHelper output) : TestHelper("AssemblyRedirection", output)
{
    [Theory]
    [Trait("Category", "EndToEnd")]
#if NET8_0
    // Case 1: Lower version should be redirected with/without native profiler
    [InlineData("8.0.0", "System.Diagnostics.DiagnosticSource", "10.0.0.0", "10.0.25.52411", true)]
    [InlineData("8.0.0", "System.Diagnostics.DiagnosticSource", "10.0.0.0", "10.0.25.52411", false)]
    // Case 2: Equal version, should NOT be redirected with/without native profiler
    [InlineData("10.0.0", "System.Diagnostics.DiagnosticSource", "10.0.0.0", "10.0.25.52411", true)]
    [InlineData("10.0.0", "System.Diagnostics.DiagnosticSource", "10.0.0.0", "10.0.25.52411", false)]
    // Case 3: Higher version should NOT be redirected with/without native profiler
    // TODO even LibraryVersion=10.0.2 loads assembly 10.0.0.0, making it identical to case 2 from the loaded assembly perspective
#elif NET9_0
    // Case 1: Lower version should be redirected with/without native profiler
    [InlineData("9.0.0", "System.Diagnostics.DiagnosticSource", "10.0.0.0", "10.0.25.52411", true)]
    [InlineData("9.0.0", "System.Diagnostics.DiagnosticSource", "10.0.0.0", "10.0.25.52411", false)]
    // Case 2: Equal version, should NOT be redirected with/without native profiler
    [InlineData("10.0.0", "System.Diagnostics.DiagnosticSource", "10.0.0.0", "10.0.25.52411", true)]
    [InlineData("10.0.0", "System.Diagnostics.DiagnosticSource", "10.0.0.0", "10.0.25.52411", false)]
    // Case 3: Higher version should NOT be redirected with/without native profiler
    // TODO even LibraryVersion=10.0.2 loads assembly 10.0.0.0, making it identical to case 2 from the loaded assembly perspective
#elif NET10_0
    // Case 1: Lower version is not possible for DiagnosticSource on .NET 10,
    //         msbuild will ignore a lower version of this package since it's part of .NET 10 SDK
    // Case 2: Equal version, should NOT be redirected with/without native profiler
    // TODO currently different test jobs use different versions of .net runtime:
    //   - test-build-container (ubuntu-22.04, alpine, alpine-x64, linux-musl): DS file version 10.0.426.12010
    //   - test-build-managed (net10.0, windows-2022): DS file version 10.0.225.61305
    [InlineData("10.0.2", "System.Diagnostics.DiagnosticSource", "10.0.0.0", "10.0.526.15411", true)]
    [InlineData("10.0.2", "System.Diagnostics.DiagnosticSource", "10.0.0.0", "10.0.526.15411", false)]
    // Case 3: Higher version is not possible for DiagnosticSource on .NET 10, the instrumentation tool is already using the highest possible version
#elif NETFRAMEWORK
    // Case 1: Lower version should be redirected before the entrypoint runs (native profiler mandatory)
    [InlineData("6.0.0", "Microsoft.Extensions.Logging.Abstractions", "10.0.0.2", "10.0.225.61305")]
    // Case 2: Equal version should keep loading the expected entrypoint assembly (native profiler mandatory)
    [InlineData("10.0.2", "Microsoft.Extensions.Logging.Abstractions", "10.0.0.2", "10.0.225.61305")]
    // Case 3: Higher version is not possible for DiagnosticSource on .NET 10, the instrumentation tool is already using the highest possible version
#endif
    public void SubmitsTraces(
        string libraryVersion,
        string expectedAssemblyName,
        string expectedAssemblyVersion,
        string expectedAssemblyFileVersion,
        bool enableNativeProfiler = true)
    {
#if NETFRAMEWORK
        Assert.True(enableNativeProfiler, "Native profiler is required for assembly redirection on .NET Framework");
        var excludedNames = string.Empty;
#else

        // on .NET (Core) Assembly Redirection without Native Profiler will load test application
        // and the startup hook twice, so we should exclude both from validation of no-duplicate loads
        var excludedNames = !enableNativeProfiler ? "TestApplication.AssemblyRedirection,OpenTelemetry.AutoInstrumentation.StartupHook" : string.Empty;
#endif
        // Arrange
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "AssemblyRedirection.ActivitySource");
        collector.Expect("AssemblyRedirection.ActivitySource");

        // Act - Configure profiler
        if (enableNativeProfiler)
        {
            EnableBytecodeInstrumentation();
        }

        // Run test application with expected version and duplicate check flag
        RunTestApplication(new TestSettings
        {
            PackageVersion = libraryVersion,
            Arguments = $"{expectedAssemblyName} {expectedAssemblyVersion} {expectedAssemblyFileVersion} {excludedNames}"
        });

        // Assert
        collector.AssertExpectations();
    }
}
