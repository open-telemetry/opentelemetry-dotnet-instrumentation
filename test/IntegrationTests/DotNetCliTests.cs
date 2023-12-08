// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if !NETFRAMEWORK

using FluentAssertions;
using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

[Trait("Category", "EndToEnd")]
public sealed class DotNetCliTests : TestHelper, IDisposable
{
    private const string DotNetCli = "dotnet";
    private const string TargetAppName = "OTelDotNetCliTest";

    private readonly string _prevWorkingDir = Directory.GetCurrentDirectory();
    private readonly DirectoryInfo _tempWorkingDir;

    public DotNetCliTests(ITestOutputHelper output)
        : base(DotNetCli, output)
    {
        var tempDirName = Path.Combine(
            Path.GetTempPath(),
            $"otel-dotnet-test-{Guid.NewGuid():N}",
            TargetAppName);
        _tempWorkingDir = Directory.CreateDirectory(tempDirName);

        Directory.SetCurrentDirectory(_tempWorkingDir.FullName);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_prevWorkingDir);
        _tempWorkingDir.Delete(recursive: true);
    }

    [Fact]
    public void WorkFlow()
    {
        // Ensure no MS telemetry spans.
        SetEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", "1");

        // Stop all build servers to ensure user like experience.
        // Currently there is an issue trying to launch VBCSCompiler background server.
        RunDotNetCli("build-server shutdown");

        var tfm = $"net{Environment.Version.Major}.0";
        RunDotNetCli($"new console --framework {tfm}");

        ChangeDefaultProgramToHttpClient();

        RunDotNetCli("build");

        var targetAppDllPath = Path.Combine(".", "bin", "Debug", tfm, TargetAppName + ".dll");
        RunAppWithDotNetCliAndAssertHttpSpans(targetAppDllPath);

        // Not necessary, but, testing a common command.
        RunDotNetCli("clean");

        RunAppWithDotNetCliAndAssertHttpSpans("run -c Release");
    }

    private static void ChangeDefaultProgramToHttpClient()
    {
        const string ProgramContent = @"
using var httpClient = new HttpClient();
httpClient.Timeout = TimeSpan.FromSeconds(5);
using var response = await httpClient.GetAsync(""http://example.com"");
Console.WriteLine(response.StatusCode);
";

        File.WriteAllText("Program.cs", ProgramContent);
    }

    private void RunDotNetCli(string arguments)
    {
        Output.WriteLine($"Running: {DotNetCli} {arguments}");

        using var process = InstrumentedProcessHelper.Start(DotNetCli, arguments, EnvironmentHelper);
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

    private void RunAppWithDotNetCliAndAssertHttpSpans(string arguments)
    {
        var collector = new MockSpansCollector(Output);
        SetExporter(collector);

#if NET7_0_OR_GREATER
        collector.Expect("System.Net.Http");
#else
        collector.Expect("OpenTelemetry.Instrumentation.Http.HttpClient");
#endif

        RunDotNetCli(arguments);

        collector.AssertExpectations();
    }
}
#endif
