// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;

namespace IntegrationTests;

public class AssemblyRedirectionTests(ITestOutputHelper output) : TestHelper("AssemblyRedirection", output)
{
    private const string AssemblyName = "System.Diagnostics.DiagnosticSource";

    [Theory]
    [Trait("Category", "EndToEnd")]
#if NET10_0
    // Case 1: Redirect the lower framework version to the distribution's DiagnosticSource 11 with the native profiler.
    // StartupHook-only keeps DiagnosticSource in the Default ALC; AdditionalDepsFallback covers upgrading that copy.
    [InlineData("10.0.15", AssemblyName, "11.0.0.0", "11.0.26.53018", true)]
    // Case 2: Equal version, should NOT be redirected with/without native profiler
    [InlineData("11.0.0", AssemblyName, "11.0.0.0", "11.0.26.53018", true)]
    [InlineData("11.0.0", AssemblyName, "11.0.0.0", "11.0.26.53018", false)]
#elif NET11_0
    // Case 1: Lower version is not possible for DiagnosticSource on .NET 11,
    //         msbuild will ignore a lower version of this package since it's part of .NET 11 SDK
    // Case 2: Equal version, should NOT be redirected with/without native profiler
    // .NET 11.0.0 ships DiagnosticSource with file version 11.0.26.53018.
    [InlineData("11.0.0", AssemblyName, "11.0.0.0", "11.0.26.53018", true)]
    [InlineData("11.0.0", AssemblyName, "11.0.0.0", "11.0.26.53018", false)]
#elif NETFRAMEWORK
    // Case 1: Lower version should be redirected (native profiler mandatory)
    [InlineData("6.0.0", AssemblyName, "11.0.0.0", "11.0.26.53018")]
    [InlineData("10.0.15", AssemblyName, "11.0.0.0", "11.0.26.53018")]
    // Case 2: Equal version, should NOT be redirected (native profiler mandatory)
    [InlineData("11.0.0", AssemblyName, "11.0.0.0", "11.0.26.53018")]
#endif
    public void DefaultRedirection(
        string libraryVersion,
        string expectedAssemblyName,
        string expectedAssemblyVersion,
        string expectedAssemblyFileVersion,
        bool enableNativeProfiler = true)
    {
#if NET10_0
        // Validate the application's dependencies and native redirection without the shared fallback.
        SetEnvironmentVariable("DOTNET_ADDITIONAL_DEPS", null);
        SetEnvironmentVariable("DOTNET_SHARED_STORE", null);
#endif
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
            Arguments = $"--assembly-name {expectedAssemblyName} --assembly-version {expectedAssemblyVersion} --assembly-file-version {expectedAssemblyFileVersion} --excluded-assemblies {excludedNames}"
        });

        // Assert
        collector.AssertExpectations();
    }

#if NET
    [Theory]
    [Trait("Category", "EndToEnd")]
    // AdditionalDeps supplies DiagnosticSource 11 on .NET 10; .NET 11 uses its shared framework copy.
    [InlineData("10.0.15", "11.0.0.0", "11.0.26.53018")]
    public void AdditionalDepsFallback(
        string libraryVersion,
        string expectedAssemblyVersion,
        string expectedAssemblyFileVersion)
    {
        var generatedDirectory = Path.Combine(
            EnvironmentTools.GetSolutionDirectory(),
            "test-artifacts",
            "additional-deps",
            Guid.NewGuid().ToString("N"));

        try
        {
            AdditionalDepsHelper.Generate(generatedDirectory, Output);

            using var collector = new MockSpansCollector(Output);
            SetExporter(collector);
            SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "AssemblyRedirection.ActivitySource");
            SetEnvironmentVariable("OTEL_DOTNET_AUTO_REDIRECT_ENABLED", "false");
            SetEnvironmentVariable("DOTNET_ADDITIONAL_DEPS", Path.Combine(generatedDirectory, "AdditionalDeps"));
            SetEnvironmentVariable("DOTNET_SHARED_STORE", Path.Combine(generatedDirectory, "store"));
            collector.Expect("AssemblyRedirection.ActivitySource");

            RunTestApplication(new TestSettings
            {
                PackageVersion = libraryVersion,
                Arguments = $"--assembly-name {AssemblyName} --assembly-version {expectedAssemblyVersion} --assembly-file-version {expectedAssemblyFileVersion}"
            });

            collector.AssertExpectations();
        }
        finally
        {
            if (Directory.Exists(generatedDirectory))
            {
                Directory.Delete(generatedDirectory, recursive: true);
            }
        }
    }

#endif

#if NET10_0
    [Fact]
    [Trait("Category", "EndToEnd")]
    public void StartupHookOnlyFailsWithoutCompatibleDiagnosticSource()
    {
        // DiagnosticSource must stay in the Default ALC on .NET 10. Without a compatible
        // application dependency or the host-level fallback, initialization is expected to fail.
        SetEnvironmentVariable("DOTNET_ADDITIONAL_DEPS", null);
        SetEnvironmentVariable("DOTNET_SHARED_STORE", null);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_FAIL_FAST_ENABLED", "true");

        var (_, errorOutput, _) = RunTestApplication(
            new TestSettings { PackageVersion = "10.0.15" },
            expectedExitCode: 0,
            assertExitCode: Assert.NotEqual);

        Assert.Contains("System.Diagnostics.DiagnosticSource, Version=11.0.0.0", errorOutput, StringComparison.Ordinal);
    }
#endif
}
