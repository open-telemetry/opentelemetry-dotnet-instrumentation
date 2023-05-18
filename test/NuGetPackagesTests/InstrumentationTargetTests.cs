// <copyright file="InstrumentationTargetTests.cs" company="OpenTelemetry Authors">
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

using FluentAssertions;
using IntegrationTests.Helpers;
using Microsoft.Build.Evaluation;
using Xunit.Abstractions;

namespace IntegrationTests;

[Trait("Category", "EndToEnd")]
public sealed class InstrumentationTargetTests : TestHelper, IDisposable
{
    private const string DotNetCli = "dotnet";
    private const string TargetAppName = "InstrumentationTargetTest";

    private readonly string _prevWorkingDir = Directory.GetCurrentDirectory();
    private readonly DirectoryInfo _tempWorkingDir;

    public InstrumentationTargetTests(ITestOutputHelper output)
        : base(DotNetCli, output)
    {
        var tempDirName = Path.Combine(
            Path.GetTempPath(),
            $"instr-target-test-{Guid.NewGuid():N}",
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
    public void InstrumentApplication()
    {
        // Ensure no MS telemetry spans.
        SetEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", "1");

        // Stop all build servers to ensure user like experience.
        // Currently there is an issue trying to launch VBCSCompiler background server.
        RunDotNetCli("build-server shutdown").Should().Be(0);

        RunDotNetCli($"new console --framework net6.0").Should().Be(0);

        ChangeDefaultProgramToHelloWorld();

        ChangeProjectDefaults();

        RunDotNetCli("build").Should().Be(0);

        // Find the directory with the NuGet packages.
        var nugetArtifactsDir = Path.GetDirectoryName(
            Path.Combine(GetTestAssemblyPath(), "../../../../../bin/nuget-artifacts/"));

        // Add the automatic instrumentation NuGet package to the app.
        RunDotNetCli(
            $"add package OpenTelemetry.AutoInstrumentation --source \"{nugetArtifactsDir}\" --prerelease").Should().Be(0);

        RunDotNetCli("build").Should().Be(0);
    }

    private void ChangeDefaultProgramToHelloWorld()
    {
        const string ProgramContent = """
            public static class Program
            {
                public static void Main()
                {
                    System.Console.WriteLine("Hello World!");
                }
            }
            
            """;

        File.WriteAllText("Program.cs", ProgramContent);
    }

    private void ChangeProjectDefaults()
    {
        string tfm;
#if NETFRAMEWORK
        tfm = "net462";
#else
        tfm = $"net{Environment.Version.Major}.0";
#endif

        var projectFile = $"{TargetAppName}.csproj";
        var projectText = File.ReadAllText(projectFile);
        projectText = projectText.Replace("<TargetFramework>net6.0</TargetFramework>", $"<TargetFramework>{tfm}</TargetFramework>");
        projectText = projectText.Replace("<ImplicitUsings>enable</ImplicitUsings>", $"<ImplicitUsings>disable</ImplicitUsings>");
        projectText = projectText.Replace("<Nullable>enable</Nullable>", $"<Nullable>disable</Nullable>");
        File.WriteAllText(projectFile, projectText);
    }

    private int RunDotNetCli(string arguments)
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
        return process.ExitCode;
    }
}
