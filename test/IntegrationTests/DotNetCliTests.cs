// <copyright file="DotNetCliTests.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

#if !NETFRAMEWORK

using System.Runtime.InteropServices;
using FluentAssertions;
using IntegrationTests.Helpers;
using Org.BouncyCastle.Asn1.X509;
using Xunit.Abstractions;
using Timeout = IntegrationTests.Helpers.Timeout;

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

    [Fact]
    public void ExecuteDotNetCliWorkFlow()
    {
        var tfm = $"net{Environment.Version.Major}.0";
        RunDotNetCli($"new console --framework {tfm}");

        ChangeDefaultProgramToHttpClient();

        RunDotNetCli("build");

        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

#if NET7_0_OR_GREATER
        collector.Expect("System.Net.Http");
#else
        collector.Expect("OpenTelemetry.Instrumentation.Http.HttpClient");
#endif

        // "dotnet run" is not supported, however, "dotnet <dll>" is expected to work.
        var targetAppDllPath = Path.Combine(".", "bin", "Debug", tfm, TargetAppName + ".dll");
        RunDotNetCli(targetAppDllPath);

        collector.AssertExpectations();
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_prevWorkingDir);
        _tempWorkingDir.Delete(recursive: true);
    }

    private static void ChangeDefaultProgramToHttpClient()
    {
        const string ProgramContent = @"
using var httpClient = new HttpClient();
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

        bool processTimeout = !process!.WaitForExit((int)Timeout.ProcessExit.TotalMilliseconds);
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
}
#endif
