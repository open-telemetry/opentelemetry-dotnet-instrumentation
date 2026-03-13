// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

public class AssemblyRedirectionTests(ITestOutputHelper output) : TestHelper("AssemblyRedirection", output)
{
    private const string AssemblyName = "System.Diagnostics.DiagnosticSource";

    [Theory]
    [Trait("Category", "EndToEnd")]
#if NET8_0
    // Case 1: Lower version should be redirected with/without native profiler
    [InlineData("8.0.0", AssemblyName, "10.0.0.0", "10.0.25.52411", true)]
    [InlineData("8.0.0", AssemblyName, "10.0.0.0", "10.0.25.52411", false)]
    // Case 2: Equal version, should NOT be redirected with/without native profiler
    [InlineData("10.0.0", AssemblyName, "10.0.0.0", "10.0.25.52411", true)]
    [InlineData("10.0.0", AssemblyName, "10.0.0.0", "10.0.25.52411", false)]
    // Case 3: Higher version should NOT be redirected with/without native profiler
    // TODO even LibraryVersion=10.0.2 loads assembly 10.0.0.0, making it identical to case 2 from the loaded assembly perspective
#elif NET9_0
    // Case 1: Lower version should be redirected with/without native profiler
    [InlineData("9.0.0", AssemblyName, "10.0.0.0", "10.0.25.52411", true)]
    [InlineData("9.0.0", AssemblyName, "10.0.0.0", "10.0.25.52411", false)]
    // Case 2: Equal version, should NOT be redirected with/without native profiler
    [InlineData("10.0.0", AssemblyName, "10.0.0.0", "10.0.25.52411", true)]
    [InlineData("10.0.0", AssemblyName, "10.0.0.0", "10.0.25.52411", false)]
    // Case 3: Higher version should NOT be redirected with/without native profiler
    // TODO even LibraryVersion=10.0.2 loads assembly 10.0.0.0, making it identical to case 2 from the loaded assembly perspective
#elif NET10_0
    // Case 1: Lower version is not possible for DiagnosticSource on .NET 10, the msbuild will ignore a version of this package since SDK contains a higher
    // Case 2: Equal version, should NOT be redirected with/without native profiler
    [InlineData("10.0.2", AssemblyName, "10.0.0.0", "10.0.225.61305", true)]
    [InlineData("10.0.2", AssemblyName, "10.0.0.0", "10.0.225.61305", false)]
    // Case 3: Higher version is not possible for DiagnosticSource on .NET 10, the instrumentation tool is already using the highest possible version
#elif NETFRAMEWORK
    // Case 1: Lower version should be redirected (native profiler mandatory)
    [InlineData("6.0.0", AssemblyName, "10.0.0.2", "10.0.225.61305")]
    // Case 2: Equal version, should NOT be redirected (native profiler mandatory)
    [InlineData("10.0.2", AssemblyName, "10.0.0.2", "10.0.225.61305")]
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
        var duplicateCheckPatterns = string.Empty;
#else

        // on .NET (Core) Assembly Redirection without Native Profiler
        // cannot guarantee full isolation, so there will be packages
        // in both Default and Isolated ALC, e.g. TestApplication library and others
        // so we should check that the important libraries are loaded once
        var duplicateCheckPatterns = !enableNativeProfiler ? string.Join(',', [expectedAssemblyName, "OpenTelemetry*"]) : string.Empty;
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
            Arguments = $"{expectedAssemblyName} {expectedAssemblyVersion} {expectedAssemblyFileVersion} {duplicateCheckPatterns}"
        });

        // Assert
        collector.AssertExpectations();
    }
}
