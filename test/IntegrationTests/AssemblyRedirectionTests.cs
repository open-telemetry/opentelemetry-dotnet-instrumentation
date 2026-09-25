// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;

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
    // TODO even LibraryVersion=10.0.2 loads assembly 10.0.0.0, making it identical to case 2 from the loaded assembly perspectives
#elif NET10_0
    // Case 1: Lower version is not possible for DiagnosticSource on .NET 10,
    //         msbuild will ignore a lower version of this package since it's part of .NET 10 SDK
    // Case 2: Equal version, should NOT be redirected with/without native profiler
    // TODO currently different test jobs use different versions of .net runtime:
    //   - test-build-container (ubuntu-22.04, alpine, alpine-x64, linux-musl): DS file version 10.0.426.12010
    //   - test-build-managed (net10.0, windows-2022): DS file version 10.0.1226.42308
    [InlineData("10.0.12", AssemblyName, "10.0.0.0", "10.0.1226.42308", true)]
    [InlineData("10.0.12", AssemblyName, "10.0.0.0", "10.0.1226.42308", false)]
    // Case 3: Higher version is not possible for DiagnosticSource on .NET 10, the instrumentation tool is already using the highest possible version
#elif NETFRAMEWORK
    // Case 1: Lower version should be redirected (native profiler mandatory)
    [InlineData("6.0.0", AssemblyName, "10.0.0.12", "10.0.1226.42308")]
    // Case 2: Equal version, should NOT be redirected (native profiler mandatory)
    [InlineData("10.0.12", AssemblyName, "10.0.0.12", "10.0.1226.42308")]
    // Case 3: Higher version is not possible for DiagnosticSource on .NET 10, the instrumentation tool is already using the highest possible version
#endif
    public void DefaultRedirection(
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
            Arguments = $"--assembly-name {expectedAssemblyName} --assembly-version {expectedAssemblyVersion} --assembly-file-version {expectedAssemblyFileVersion} --excluded-assemblies {excludedNames}"
        });

        // Assert
        collector.AssertExpectations();
    }

#if !NETFRAMEWORK
    [Theory]
    [Trait("Category", "EndToEnd")]
#if NET8_0
    [InlineData("8.0.0", "10.0.0.0", "10.0.25.52411")]
#elif NET9_0
    [InlineData("9.0.0", "10.0.0.0", "10.0.25.52411")]
#elif NET10_0
    [InlineData("10.0.12", "10.0.0.0", "10.0.1226.42308")]
#endif
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
            GenerateAdditionalDeps(generatedDirectory);

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

    private void GenerateAdditionalDeps(string outputDirectory)
    {
        var tracerHome = EnvironmentHelper.GetNukeBuildOutput();
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // The same target-framework test assembly runs on Windows and Unix, so host selection must be
        // made at runtime rather than with target-framework compilation symbols.
        if (EnvironmentTools.IsWindows())
        {
            startInfo.FileName = "powershell.exe";
            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-NonInteractive");
            startInfo.ArgumentList.Add("-ExecutionPolicy");
            startInfo.ArgumentList.Add("Bypass");
            startInfo.ArgumentList.Add("-File");
            startInfo.ArgumentList.Add(Path.Combine(tracerHome, "generate-additional-deps.ps1"));
            startInfo.ArgumentList.Add("-OutputPath");
            startInfo.ArgumentList.Add(outputDirectory);
        }
        else
        {
            startInfo.FileName = "/bin/sh";
            startInfo.ArgumentList.Add(Path.Combine(tracerHome, "generate-additional-deps.sh"));
            startInfo.ArgumentList.Add("--output");
            startInfo.ArgumentList.Add(outputDirectory);
        }

        using var process = System.Diagnostics.Process.Start(startInfo);
        Assert.NotNull(process);
        using var helper = new ProcessHelper(process);

        var exitedInTime = process!.WaitForExit((int)TestTimeout.ProcessExit.TotalMilliseconds);
        if (!exitedInTime)
        {
            process.Kill();
            process.WaitForExit();
        }

        Output.WriteLine($"AdditionalDeps generator exit code: {process.ExitCode}");
        Output.WriteResult(helper);

        Assert.True(exitedInTime, "AdditionalDeps generator timed out");
        Assert.Equal(0, process.ExitCode);
    }
#endif
}
