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
using NuGet.Versioning;
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
        // Disable dotnet CLI telemetry.
        SetEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", "1");

        // Always create the app targeting a fixed framework version to simplify
        // text replacement in the project file.
        RunDotNetCli($"new console --framework net6.0").Should().Be(0);

        ChangeDefaultProgramToHelloWorld();

        ChangeProjectDefaultsAndTargetFranework();

        RunDotNetCli("build").Should().Be(0);

        // Add the automatic instrumentation NuGet package to the app.
        var nugetArtifactsDir = Path.Combine(GetTestAssemblyPath(), "../../../../../bin/nuget-artifacts/");
        RunDotNetCli(
            $"add package OpenTelemetry.AutoInstrumentation --source \"{nugetArtifactsDir}\" --prerelease").Should().Be(0);

        RunDotNetCli("build").Should().Be(0);

        // Read the auto-instrumentation targets file and verify detection, ignoring, and instrumentation of targets.
        var otelAutoTargetsFile = Path.Combine(GetTestAssemblyPath(), "OpenTelemetry.AutoInstrumentation.BuildTasks.targets");
        var otelAutoTargets = new Project(otelAutoTargetsFile);
        foreach (var instrTarget in otelAutoTargets.Xml.Items.Where(x => x.ElementName == "InstrumentationTarget"))
        {
            // Add instrumentation target to the app.
            var instrTargetVersionRange = VersionRange.Parse(
                instrTarget.Metadata.Single(x => x.Name == "TargetNuGetPackageVersionRange").Value);
            RunDotNetCli($"add package {instrTarget.Include} --version {instrTargetVersionRange.MinVersion}").Should().Be(0);

            // Build should fail because the target is not yet instrumented.
            var instrPackageId = instrTarget.Metadata.Single(x => x.Name == "InstrumentationNuGetPackageId").Value;
            var instrPackageVersion = instrTarget.Metadata.Single(x => x.Name == "InstrumentationNuGetPackageVersion").Value;
            var expectedErrorMessage =
                $"OpenTelemetry.AutoInstrumentation: add a reference to the instrumentation package '{instrPackageId}' version " +
                $"{instrPackageVersion} or add '{instrTarget.Include}' to the property 'DisabledInstrumentations' to suppress this error.";

            RunDotNetCli("build", expectedErrorMessage).Should().NotBe(0);

            // Explicitly disable the instrumentation target.
            RunDotNetCli($"build -p:DisabledInstrumentations={instrTarget.Include}").Should().Be(0);

            // Add the instrumentation package, build should succeed.
            RunDotNetCli($"add package {instrPackageId} --version {instrPackageVersion}").Should().Be(0);
            RunDotNetCli("build").Should().Be(0);
        }
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

    private void ChangeProjectDefaultsAndTargetFranework()
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

    private int RunDotNetCli(string arguments, string? expectedOutputFragment = null)
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

        if (expectedOutputFragment is not null)
        {
            helper.StandardOutput.Should().Contain(expectedOutputFragment);
        }

        processTimeout.Should().BeFalse("Test application timed out");
        return process.ExitCode;
    }
}
