// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using FluentAssertions;
using IntegrationTests.Helpers;
using Microsoft.Build.Evaluation;
using NuGet.Versioning;
using Xunit.Abstractions;

namespace IntegrationTests;

[Trait("Category", "EndToEnd")]
public sealed class InstrumentationTargetTests : TestHelper
{
    private const string DotNetCli = "dotnet";
    private const string TargetAppName = "InstrumentationTargetTest";

    public InstrumentationTargetTests(ITestOutputHelper output)
        : base(DotNetCli, output, "nuget-packages")
    {
    }

#if NET
    [Fact]
    public void PublishApplication()
    {
        RunInTempDir(() =>
        {
            var tfm = $"net{Environment.Version.Major}.0";
            CreateTestApp(tfm);

            // Warning should be logged when runtime identifier is not specified
            RunDotnetPublish(arguments: string.Empty, expectWarning: true);

            // No warning should be logged when runtime identifier is specified
            RunDotnetPublish(arguments: "--ucr", expectWarning: false);

            // No warning should be logged when runtime identifier is not specified and check is disabled
            RunDotnetPublish(arguments: "-p:DisableAutoInstrumentationCheckForRuntimeIdentifier=true", expectWarning: false);
        });
    }

#endif

    [Fact]
    public void InstrumentApplication()
    {
        RunInTempDir(() =>
        {
            string tfm;
#if NETFRAMEWORK
            tfm = "net462";
#else
            tfm = $"net{Environment.Version.Major}.0";
#endif
            CreateTestApp(tfm);

            // Read the auto-instrumentation targets file and verify detection, ignoring, and instrumentation of targets.
            var projectProperties = new Dictionary<string, string> { { "TargetFramework", tfm }, };

            var otelAutoTargetsFile = Path.Combine(
                GetTestAssemblyPath(),
                "OpenTelemetry.AutoInstrumentation.BuildTasks.targets");
            var otelAutoTargets = new Project(otelAutoTargetsFile, projectProperties, null);
            foreach (var instrTarget in otelAutoTargets.Items.Where(x => x.ItemType == "InstrumentationTarget"))
            {
                // Add instrumentation target to the app.
                var instrTargetVersionRange = VersionRange.Parse(
                    instrTarget.Metadata.Single(x => x.Name == "TargetNuGetPackageVersionRange").EvaluatedValue);
                RunDotNetCli(
                        $"add package {instrTarget.EvaluatedInclude} --version {instrTargetVersionRange.MinVersion}")
                    .Should().Be(0);

                // Build should fail because the target is not yet instrumented.
                var instrPackageId = instrTarget.Metadata.Single(x => x.Name == "InstrumentationNuGetPackageId")
                    .EvaluatedValue;
                var instrPackageVersion = instrTarget.Metadata
                    .Single(x => x.Name == "InstrumentationNuGetPackageVersion").EvaluatedValue;
                var expectedErrorMessage =
                    $"OpenTelemetry.AutoInstrumentation: add a reference to the instrumentation package '{instrPackageId}' version " +
                    $"{instrPackageVersion} or add '{instrTarget.EvaluatedInclude}' to the property 'SkippedInstrumentations' to suppress this error.";

                RunDotNetCli("build", expectedErrorMessage).Should().NotBe(0);

                // Explicitly disable the instrumentation target.
                RunDotNetCli($"build -p:SkippedInstrumentations={instrTarget.EvaluatedInclude}").Should().Be(0);

                // Add the instrumentation package, build should succeed.
                RunDotNetCli($"add package {instrPackageId} --version {instrPackageVersion}").Should().Be(0);
                RunDotNetCli("build").Should().Be(0);
            }
        });
    }

    private static void RunInTempDir(Action action)
    {
        string? prevWorkingDir = null;
        DirectoryInfo? tempWorkingDir = null;
        try
        {
            prevWorkingDir = Directory.GetCurrentDirectory();
            var tempDirName = Path.Combine(
                Path.GetTempPath(),
                $"instr-target-test-{Guid.NewGuid():N}",
                TargetAppName);

            tempWorkingDir = Directory.CreateDirectory(tempDirName);
            Directory.SetCurrentDirectory(tempDirName);

            action();
        }
        finally
        {
            if (prevWorkingDir != null)
            {
                Directory.SetCurrentDirectory(prevWorkingDir);
            }

            tempWorkingDir?.Parent?.Delete(true);
        }
    }

#if NET
    private void RunDotnetPublish(string arguments, bool expectWarning)
    {
        const string warningMessage = "RuntimeIdentifier (RID) is not set." +
                                      " Consider setting it to avoid copying native libraries for all of the platforms supported by the OpenTelemetry.AutoInstrumentation package." +
                                      " See the docs at https://opentelemetry.io/docs/zero-code/net/nuget-packages/#using-the-nuget-packages for details." +
                                      " In order to suppress this warning, set DisableAutoInstrumentationCheckForRuntimeIdentifier property to true.";

        var (exitCode, standardOutput) = RunDotnetCliAndWaitForCompletion($"publish {arguments}");
        exitCode.Should().Be(0);
        if (expectWarning)
        {
            standardOutput.Should().Contain(warningMessage);
        }
        else
        {
            standardOutput.Should().NotContain(warningMessage);
        }
    }
#endif

    private void CreateTestApp(string tfm)
    {
        // Disable dotnet CLI telemetry.
        SetEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", "1");

        // Always create the app targeting a fixed framework version to simplify
        // text replacement in the project file.
        RunDotNetCli($"new console --framework net8.0").Should().Be(0);

        ChangeProjectDefaultsAndTargetFramework(tfm);

        ChangeDefaultProgramToHelloWorld();

        RunDotNetCli("build").Should().Be(0);

        // Add the automatic instrumentation NuGet package to the app. Because the package has dependencies to other
        // packages that may not be present yet, `dotnet add package OpenTelemetry.AutoInstrumentation --source <src>`
        // may fail (the command doesn't support multiple sources). Workaround the issue by creating a nuget.config
        // file and adding the proper source.
        RunDotNetCli("new nugetconfig").Should().Be(0);
        var nugetArtifactsDir = Path.Combine(GetTestAssemblyPath(), "../../../../../bin/nuget-artifacts/");
        RunDotNetCli(
            $"nuget add source \"{nugetArtifactsDir}\" --name nuget-artifacts --configfile nuget.config").Should().Be(0);
        RunDotNetCli(
            $"add package OpenTelemetry.AutoInstrumentation --prerelease").Should().Be(0);

        RunDotNetCli("build").Should().Be(0);
    }

    private void ChangeDefaultProgramToHelloWorld()
    {
        const string programContent = """

                                      public static class Program
                                      {
                                          public static void Main()
                                          {
                                              System.Console.WriteLine("Hello World!");
                                          }
                                      }

                                      """;

        File.WriteAllText("Program.cs", programContent);
    }

    private void ChangeProjectDefaultsAndTargetFramework(string tfm)
    {
        var projectFile = $"{TargetAppName}.csproj";
        var projectText = File.ReadAllText(projectFile);
        projectText = projectText.Replace("<TargetFramework>net8.0</TargetFramework>", $"<TargetFramework>{tfm}</TargetFramework>");
        projectText = projectText.Replace("<ImplicitUsings>enable</ImplicitUsings>", $"<ImplicitUsings>disable</ImplicitUsings>");
        projectText = projectText.Replace("<Nullable>enable</Nullable>", $"<Nullable>disable</Nullable>");
        File.WriteAllText(projectFile, projectText);
    }

    private int RunDotNetCli(string arguments, string? expectedOutputFragment = null)
    {
        var (exitCode, standardOutput) = RunDotnetCliAndWaitForCompletion(arguments);
        if (expectedOutputFragment is not null)
        {
            standardOutput.Should().Contain(expectedOutputFragment);
        }

        return exitCode;
    }

    private (int ExitCode, string StandardOutput) RunDotnetCliAndWaitForCompletion(string arguments)
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
        return (process.ExitCode, helper.StandardOutput);
    }
}
