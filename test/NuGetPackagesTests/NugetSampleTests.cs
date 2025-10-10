// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;
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

        Assert.Single(childrenDirs);

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
            return "osx-arm64";
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
            using var testServer = TestHttpServer.CreateDefaultTestServer(Output);
            var dllName = EnvironmentHelper.FullTestApplicationName.EndsWith(".exe")
                ? EnvironmentHelper.FullTestApplicationName.Replace(".exe", ".dll")
                : EnvironmentHelper.FullTestApplicationName + ".dll";
            var dllPath = Path.Combine(appDir, dllName);
            RunInstrumentationTarget($"dotnet {dllPath} --test-server-port {testServer.Port}", appDir);
        });
    }
#endif

    private void InstrumentExecutable(string appDir)
    {
        RunAndAssertHttpSpans(() =>
        {
            using var testServer = TestHttpServer.CreateDefaultTestServer(Output);
            var instrumentationTarget = Path.Combine(appDir, EnvironmentHelper.FullTestApplicationName);
            instrumentationTarget = EnvironmentTools.IsWindows() ? instrumentationTarget + ".exe" : instrumentationTarget;
            RunInstrumentationTarget($"{instrumentationTarget} --test-server-port {testServer.Port}", appDir);
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

        Assert.NotNull(process);

        bool processTimeout = !process!.WaitForExit((int)TestTimeout.ProcessExit.TotalMilliseconds);
        if (processTimeout)
        {
            process.Kill();
        }

        Output.WriteLine("ProcessId: " + process.Id);
        Output.WriteLine("Exit Code: " + process.ExitCode);
        Output.WriteResult(helper);

        Assert.False(processTimeout, "Test application timed out");
        Assert.Equal(0, process.ExitCode);
    }

    private void RunAndAssertHttpSpans(Action appLauncherAction)
    {
        using var collector = new MockSpansCollector(Output);
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
