// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

// This test is defined in NET 9.0 because the tool is written in .NET 9.0
// The actual test is testing .NET Framework context.
#if NET10_0

using System.Reflection;
using System.Runtime.InteropServices;
using DependencyListGenerator;
using Microsoft.Build.Evaluation;
using NuGet.Configuration;
using NuGet.Frameworks;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests;

public class FrameworkDistroTests
{
    private const string AssembliesProjectName = "OpenTelemetry.AutoInstrumentation.Assemblies";

    [Fact]
    public void GeneratorDiscoversTransientDependencies()
    {
        var currentTestLocation = Assembly.GetExecutingAssembly().Location;
        var testDir = FindParentDir(currentTestLocation, "test");
        var srcDir = Path.Combine(Directory.GetParent(testDir)!.FullName, "src");
        var codeDir = Path.Combine(srcDir, AssembliesProjectName);
        var projectPath = Path.Combine(codeDir, $"{AssembliesProjectName}.csproj");

        var frameworkDependencies = Generator.EnumerateDependencies(projectPath);

        // Check that every TFM has 'Microsoft.Extensions.Configuration.Binder' reference.
        // We just test one common transient dependency for all
        Assert.All(frameworkDependencies, pair => Assert.Contains(pair.Value, it => it.Name == "Microsoft.Extensions.Configuration.Binder"));
    }

    [Fact]
    public void ValidateTransientDependenciesPackageVersionsDefined()
    {
        var currentTestLocation = Assembly.GetExecutingAssembly().Location;
        var testDir = FindParentDir(currentTestLocation, "test");
        var srcDir = Path.Combine(Directory.GetParent(testDir)!.FullName, "src");
        var codeDir = Path.Combine(srcDir, AssembliesProjectName);
        var projectPath = Path.Combine(codeDir, $"{AssembliesProjectName}.csproj");
        var packagesPath = Path.Combine(codeDir, "Directory.Packages.props");

        var frameworkDependencies = Generator.EnumerateDependencies(projectPath);

        // We use "15.0" as toolkit version, it should be replaced with "Current"
        // when Microsoft.Build will be updated.
        const string toolsVersion = "15.0";

        Assert.All(frameworkDependencies, pair =>
        {
            using var collection = new ProjectCollection();
            // MSBuild needs both TargetFramework (e.g., "net462") and TargetFrameworkIdentifier (e.g., ".NETFramework")
            // because Directory.Packages.props uses both conditions to include
            // framework-specific package versions.
            var properties = new Dictionary<string, string>
            {
                { "TargetFramework", pair.Key },
                { "TargetFrameworkIdentifier", NuGetFramework.Parse(pair.Key).Framework }
            };
            var project = new Project(packagesPath, properties, toolsVersion, collection);

            var versionedDependencies = project.Items
                .Where(it => it.ItemType == "PackageVersion")
                .Select(it => it.EvaluatedInclude)
                .ToHashSet();

            Assert.All(pair.Value, dep => Assert.Contains(dep.Name, versionedDependencies));
        });
    }

    [SkippableFact]
    public void ReferencedPackagesNoUnsupportedNetFramework()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), ".NET Framework is supported only on Windows.");

        var currentTestLocation = Assembly.GetExecutingAssembly().Location;
        var testDir = FindParentDir(currentTestLocation, "test");
        var srcDir = Path.Combine(Directory.GetParent(testDir)!.FullName, "src");
        var codeDir = Path.Combine(srcDir, AssembliesProjectName);
        var projectPath = Path.Combine(codeDir, $"{AssembliesProjectName}.csproj");

        var dependencies = Generator.EnumerateDependencies(projectPath).SelectMany(it => it.Value).ToHashSet();
        var packagesFolder = SettingsUtility.GetGlobalPackagesFolder(Settings.LoadDefaultSettings(root: codeDir));

        string[] supportedFrameworkTfmPrefixes = ["net40", "net45", "net46", "net47"];

        // We may need add new TFM in OpenTelemetry.AutoInstrumentation.Assemblies
        // (and build infrastructure, readme) if referenced dependencies has a custom build for that TFM.
        Assert.All(dependencies, dependency =>
        {
            var packageFolder = Path.Combine(packagesFolder, dependency.Name, dependency.Version);

            // Check .NET Framework TFMs
            var net4Files = Directory.EnumerateDirectories(packageFolder, "net4*", SearchOption.AllDirectories)
                .Select(Path.GetFileName)
                .Cast<string>();

            Assert.All(net4Files, file =>
                Assert.True(
                    file.Contains('.', StringComparison.Ordinal) ||
                    supportedFrameworkTfmPrefixes.Any(prefix => file.StartsWith(prefix, StringComparison.Ordinal)),
                    $"Package {dependency.Name} v{dependency.Version} contains unsupported .NET Framework TFM '{file}'"));
        });
    }

    [Fact]
    public void ReferencedPackagesNoUnsupportedNet()
    {
        var currentTestLocation = Assembly.GetExecutingAssembly().Location;
        var testDir = FindParentDir(currentTestLocation, "test");
        var srcDir = Path.Combine(Directory.GetParent(testDir)!.FullName, "src");
        var codeDir = Path.Combine(srcDir, AssembliesProjectName);
        var projectPath = Path.Combine(codeDir, $"{AssembliesProjectName}.csproj");

        var dependencies = Generator.EnumerateDependencies(projectPath).SelectMany(it => it.Value).ToHashSet();
        var packagesFolder = SettingsUtility.GetGlobalPackagesFolder(Settings.LoadDefaultSettings(root: codeDir));

        string[] supportedCoreTfmPrefixes = ["netcoreapp", "netstandard", "net5", "net6", "net7", "net8", "net9", "net10"];

        // We may need add new TFM in OpenTelemetry.AutoInstrumentation.Assemblies
        // (and build infrastructure, readme) if referenced dependencies has a custom build for that TFM.
        Assert.All(dependencies, dependency =>
        {
            var packageFolder = Path.Combine(packagesFolder, dependency.Name, dependency.Version);

            // Check .NET Core+ TFMs (netcoreapp*, netstandard*, net5.0+)
            var netCoreAppFiles = Directory.EnumerateDirectories(packageFolder, "netcoreapp*", SearchOption.AllDirectories);
            var netStandardFiles = Directory.EnumerateDirectories(packageFolder, "netstandard*", SearchOption.AllDirectories);
            // Range(5, 6) generates versions 5 through 10 (start=5, count=6)
            var net5PlusFiles = Enumerable.Range(5, 6)
                .SelectMany(version => Directory.EnumerateDirectories(packageFolder, $"net{version}.*", SearchOption.AllDirectories));

            var netCoreFiles = netCoreAppFiles
                .Concat(netStandardFiles)
                .Concat(net5PlusFiles)
                .Select(Path.GetFileName)
                .Cast<string>();

            Assert.All(netCoreFiles, file =>
                Assert.True(
                    file.Contains('.', StringComparison.Ordinal) ||
                    supportedCoreTfmPrefixes.Any(prefix => file.StartsWith(prefix, StringComparison.Ordinal)),
                    $"Package {dependency.Name} v{dependency.Version} contains unsupported .NET Core/Standard TFM '{file}'"));
        });
    }

    private static string FindParentDir(string location, string parentName)
        => Directory.GetParent(location) switch
        {
            null => throw new InvalidOperationException("Could not find parent test directory"),
            var parent when parent.Name == parentName => parent.FullName,
            var parent => FindParentDir(parent.FullName, parentName)
        };
}
#endif
