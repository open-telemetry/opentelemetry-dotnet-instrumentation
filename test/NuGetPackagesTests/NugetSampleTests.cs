// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;
using FluentAssertions;
using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

[Trait("Category", "EndToEnd")]
public sealed class NugetSampleTests : TestHelper
{
    private readonly string _ridSpecificAppDir;
    private readonly string _baseAppDir;

    public NugetSampleTests(ITestOutputHelper output)
        : base("NugetSample", output, "nuget-packages")
    {
        _baseAppDir = EnvironmentHelper
            .GetTestApplicationApplicationOutputDirectory();

        // The self-contained app is going to have an extra folder before it: the one
        // with a RID like "win-x64", "linux-x64", etc.
        var childrenDirs = Directory.GetDirectories(_baseAppDir, GetPlatformSpecificDirName());

        childrenDirs.Should().HaveCount(1);

        _ridSpecificAppDir = childrenDirs[0];
    }

    [Fact]
    public void InstrumentExecutableFromRidSpecificDir()
    {
        InstrumentExecutable(_ridSpecificAppDir);
    }

#if NET
    [Fact]
    public void InstrumentExecutableFromBaseDir()
    {
        InstrumentExecutable(_baseAppDir);
    }

    [Fact]
    public void InstrumentDllFromRidSpecificDir()
    {
        InstrumentDll(_ridSpecificAppDir);
    }

    [Fact]
    public void InstrumentDllFromBaseDir()
    {
        InstrumentDll(_baseAppDir);
    }
#endif

    private static string GetPlatformSpecificDirName()
    {
#if NETFRAMEWORK
        return "win-x64";
#else
        var runtimeIdentifier = RuntimeInformation.RuntimeIdentifier;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var processArchitecture = RuntimeInformation.ProcessArchitecture;
            switch (processArchitecture)
            {
                case Architecture.X64:
                    return "linux-x64";
                case Architecture.Arm64:
                    return "linux-arm64";
                default:
                    throw new NotSupportedException($"{processArchitecture} is not supported.");
            }
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "osx-x64";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "win-x64";
        }

        throw new NotSupportedException($"{runtimeIdentifier} is not supported.");
#endif
    }

#if NET
    private void InstrumentDll(string appDir)
    {
        RunAndAssertHttpSpans(() =>
        {
            var dllName = EnvironmentHelper.FullTestApplicationName.EndsWith(".exe")
                ? EnvironmentHelper.FullTestApplicationName.Replace(".exe", ".dll")
                : EnvironmentHelper.FullTestApplicationName + ".dll";
            var dllPath = Path.Combine(appDir, dllName);
            RunInstrumentationTarget($"dotnet {dllPath}", appDir);
        });
    }
#endif

    private void InstrumentExecutable(string appDir)
    {
        RunAndAssertHttpSpans(() =>
        {
            var instrumentationTarget = Path.Combine(appDir, EnvironmentHelper.FullTestApplicationName);
            instrumentationTarget = EnvironmentTools.IsWindows() ? instrumentationTarget + ".exe" : instrumentationTarget;
            RunInstrumentationTarget(instrumentationTarget, appDir);
        });
    }

    private void RunInstrumentationTarget(string instrumentationTarget, string appDir)
    {
        var instrumentationScriptExtension = EnvironmentTools.IsWindows() ? ".cmd" : ".sh";
        var instrumentationScriptPath =
            Path.Combine(appDir, "instrument") + instrumentationScriptExtension;

        Output.WriteLine($"Running: {instrumentationScriptPath} {instrumentationTarget}");

        using var process = InstrumentedProcessHelper.Start(instrumentationScriptPath, instrumentationTarget, EnvironmentHelper);
        using var helper = new ProcessHelper(process);

        process.Should().NotBeNull();

        bool processTimeout = !process!.WaitForExit((int)TestTimeout.ProcessExit.TotalMilliseconds);
        if (processTimeout)
        {
            process.Kill();
        }

        Output.WriteLine("ProcessId: " + process.Id);
        Output.WriteLine("Exit Code: " + process.ExitCode);
        Output.WriteResult(helper);

        processTimeout.Should().BeFalse("Test application timed out");
        process.ExitCode.Should().Be(0, "Test application exited with non-zero exit code");
    }

    private void RunAndAssertHttpSpans(Action appLauncherAction)
    {
        var collector = new MockSpansCollector(Output);
        SetExporter(collector);

#if NETFRAMEWORK
        collector.Expect("OpenTelemetry.Instrumentation.Http.HttpWebRequest");
#elif NET
        collector.Expect("System.Net.Http");
#endif

        appLauncherAction();

        collector.AssertExpectations();
    }
}
