// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

// This test is defined in NET 9.0 because the tool is written in .NET 9.0
// The actual test is testing .NET Framework context.
#if NET10_0

using System.Reflection;
using System.Runtime.InteropServices;
using DependencyListGenerator;
using NuGet.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace OpenTelemetry.AutoInstrumentation.Tests;

public class NetFrameworkDistroTests
{
    private readonly ITestOutputHelper _output;

    public NetFrameworkDistroTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [SkippableFact]
    public void GeneratorDiscoversTransientDependencies()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Supported only on Windows.");

        var currentTestLocation = Assembly.GetExecutingAssembly().Location;
        var testDir = FindParentDir(currentTestLocation, "test");
        var srcDir = Path.Combine(Directory.GetParent(testDir)!.FullName, "src");
        var codeDir = Path.Combine(srcDir, "OpenTelemetry.AutoInstrumentation.Assemblies.NetFramework");
        var projectPath = Path.Combine(codeDir, "OpenTelemetry.AutoInstrumentation.Assemblies.NetFramework.csproj");

        var dependencies = Generator.EnumerateDependencies(projectPath);

        // Check that every TFM has System.Memory reference. We just selected one transient dependency to test.
        Assert.True(dependencies.All(tfm => tfm.Value.Any(dep => dep.Name == "System.Memory")));
    }

    [SkippableFact]
    public void ValidateTransientDependenciesPackageVersionsDefined()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Supported only on Windows.");

        var currentTestLocation = Assembly.GetExecutingAssembly().Location;
        var testDir = FindParentDir(currentTestLocation, "test");
        var srcDir = Path.Combine(Directory.GetParent(testDir)!.FullName, "src");
        var codeDir = Path.Combine(srcDir, "OpenTelemetry.AutoInstrumentation.Assemblies.NetFramework");
        var projectPath = Path.Combine(codeDir, "OpenTelemetry.AutoInstrumentation.Assemblies.NetFramework.csproj");
        var packagesPath = Path.Combine(codeDir, "Directory.Packages.props");

        var discoveredDependencies = Generator.EnumerateDependencies(projectPath);

        List<(string Package, string Tfm)> undefinedDependencies = new();

        foreach (var tfmDependencies in discoveredDependencies)
        {
            using var collection = new Microsoft.Build.Evaluation.ProjectCollection();
            // We use "15.0" as toolkit version, it should be replaced with "Current"
            // when Microsoft.Build will be updated.
            var project =
                new Microsoft.Build.Evaluation.Project(
                    Path.Combine(Path.GetDirectoryName(packagesPath)!, "Directory.Packages.props"),
                    new Dictionary<string, string> { ["TargetFramework"] = tfmDependencies.Key },
                    "15.0",
                    collection);

            var definedVersions = project
                .Items.Where(it => it.ItemType == "PackageVersion").ToDictionary(item => item.EvaluatedInclude, item => item.GetMetadata("Version"));

            undefinedDependencies.AddRange(tfmDependencies.Value.Where(dep => !definedVersions.ContainsKey(dep.Name)).Select(dep => (dep.Name, tfmDependencies.Key)));
        }

        Assert.Empty(undefinedDependencies);
    }

    [SkippableFact]
    public void ReferencedPackagesNoUnsupportedTfm()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Supported only on Windows.");

        var currentTestLocation = Assembly.GetExecutingAssembly().Location;
        var testDir = FindParentDir(currentTestLocation, "test");
        var srcDir = Path.Combine(Directory.GetParent(testDir)!.FullName, "src");
        var codeDir = Path.Combine(srcDir, "OpenTelemetry.AutoInstrumentation.Assemblies.NetFramework");
        var projectPath = Path.Combine(codeDir, "OpenTelemetry.AutoInstrumentation.Assemblies.NetFramework.csproj");

        var dependencies = Generator.EnumerateDependencies(projectPath).SelectMany(tfm => tfm.Value).ToHashSet();

        var packagesFolder =
            SettingsUtility.GetGlobalPackagesFolder(
                Settings.LoadDefaultSettings(root: codeDir));

        string[] knownOldPrefix = ["net40", "net45", "net46", "net47"];
        List<string> unexpectedFrameworkFolders = [];
        foreach (var dependency in dependencies)
        {
            var packageFolder = Path.Combine(packagesFolder, dependency.Name, dependency.Version);
            foreach (var folder in Directory.EnumerateDirectories(packageFolder, "net4*", SearchOption.AllDirectories))
            {
                var tfm = Path.GetFileName(folder);
                if (tfm.Contains('.', StringComparison.Ordinal))
                {
                    // .NET 40.x
                    continue;
                }

                if (knownOldPrefix.Any(prefix => tfm.StartsWith(prefix, StringComparison.Ordinal)))
                {
                    continue;
                }

                var path = Path.GetRelativePath(packagesFolder, folder);
                unexpectedFrameworkFolders.Add(path);
                _output.WriteLine($"Unexpected TFM: {path}");
            }
        }

        // We may need add new TFM in OpenTelemetry.AutoInstrumentation.Assemblies.NetFramework
        // (and build infrastructure, readme) if referenced dependencies has a custom build for that TFM.
        Assert.Empty(unexpectedFrameworkFolders);
    }

    private static string FindParentDir(string location, string parentName)
    {
        var parent = Directory.GetParent(location);
        if (parent == null)
        {
            throw new InvalidOperationException("Could not find parent test directory");
        }

        if (parent.Name == parentName)
        {
            return parent.FullName;
        }

        return FindParentDir(parent.FullName, parentName);
    }
}
#endif
