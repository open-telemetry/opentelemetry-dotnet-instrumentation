// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

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
    public async Task WorkFlow()
    {
        using var testServer = TestHttpServer.CreateDefaultTestServer(Output);
        // Ensure no MS telemetry spans.
        SetEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", "1");

        // Stop all build servers to ensure user like experience.
        // Currently there is an issue trying to launch VBCSCompiler background server.
        RunDotNetCli("build-server shutdown");

        var tfm = $"net{Environment.Version.Major}.0";
        RunDotNetCli($"new console --framework {tfm}");

        ChangeDefaultProgramToHttpClient(testServer.Port);

        RunDotNetCli("build");

        var targetAppDllPath = Path.Combine(".", "bin", "Debug", tfm, TargetAppName + ".dll");
        await RunAppWithDotNetCliAndAssertHttpSpans(targetAppDllPath);

        // Not necessary, but, testing a common command.
        RunDotNetCli("clean");

        await RunAppWithDotNetCliAndAssertHttpSpans("run -c Release");
    }

    private static void ChangeDefaultProgramToHttpClient(int testServerPort)
    {
        var testServerAddress = $"http://localhost:{testServerPort}/test";
        var programContent = $$"""
                               using var httpClient = new HttpClient();
                               httpClient.Timeout = TimeSpan.FromSeconds(10);
                               try
                               {
                                   var response = await httpClient.GetAsync("{{testServerAddress}}");
                                   Console.WriteLine(response.StatusCode);
                               }
                               catch (Exception e)
                               {
                                   Console.WriteLine(e);
                               }
                               """;

        File.WriteAllText("Program.cs", programContent);
    }

    private void RunDotNetCli(string arguments)
    {
        Output.WriteLine($"Running: {DotNetCli} {arguments}");

        using var process = InstrumentedProcessHelper.Start(DotNetCli, arguments, EnvironmentHelper);
        using var helper = new ProcessHelper(process);

        Assert.NotNull(process);

        var processTimeout = !process!.WaitForExit((int)TestTimeout.ProcessExit.TotalMilliseconds);
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

    private async Task RunAppWithDotNetCliAndAssertHttpSpans(string arguments)
    {
        using var collector = await MockSpansCollector.InitializeAsync(Output);
        SetExporter(collector);

        collector.Expect("System.Net.Http");

        RunDotNetCli(arguments);

        collector.AssertExpectations();
    }
}
#endif
