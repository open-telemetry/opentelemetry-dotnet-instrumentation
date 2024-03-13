// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using System.Runtime.CompilerServices;
using LibraryVersionsGenerator.Models;
using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;

namespace LibraryVersionsGenerator;

public class Program
{
    private static Dictionary<string, string> _packageVersions = new Dictionary<string, string>();

    public static async Task Main()
    {
        var thisFilePath = GetSourceFilePathName();
        var solutionFolder = Path.Combine(thisFilePath, "..", "..", "..");
        var packagePropsFile = Path.Combine(solutionFolder, "test", "Directory.Packages.props");
        var project = Project.FromFile(packagePropsFile, new ProjectOptions());
        var additionalPlatforms = new List<string>();

        _packageVersions = project.GetItems("PackageVersion").ToDictionary(x => x.EvaluatedInclude, x => x.DirectMetadata.Single().EvaluatedValue);

        var xUnitFileStringBuilder = new XUnitFileBuilder();
        var buildFileStringBuilder = new BuildFileBuilder();

        xUnitFileStringBuilder.AddAutoGeneratedHeader();
        buildFileStringBuilder.AddAutoGeneratedHeader();

        xUnitFileStringBuilder.BeginClass(classNamespace: "IntegrationTests", className: "LibraryVersions");
        buildFileStringBuilder.BeginClass(classNamespace: string.Empty, className: "LibraryVersions");

        foreach (var packageVersionDefinition in PackageVersionDefinitions.Definitions)
        {
            xUnitFileStringBuilder.BeginTestPackage(packageVersionDefinition.TestApplicationName, packageVersionDefinition.IntegrationName);
            buildFileStringBuilder.BeginTestPackage(packageVersionDefinition.TestApplicationName, packageVersionDefinition.IntegrationName);

            var uniqueVersions = new HashSet<string>(packageVersionDefinition.Versions.Count);
            var platformVersions = new Dictionary<string, List<string>>();

            foreach (var version in packageVersionDefinition.Versions)
            {
                var calculatedVersion = EvaluateVersion(packageVersionDefinition.NugetPackageName, version.Version);

                if (uniqueVersions.Add(calculatedVersion))
                {
                    var isPlatformSpecific = false;

                    // Collects versions with platform specific flag
                    if (version.SupportedPlatforms.Any())
                    {
                        isPlatformSpecific = true;

                        foreach (var platform in version.SupportedPlatforms)
                        {
                            if (!platformVersions.ContainsKey(platform))
                            {
                                platformVersions.Add(platform, new List<string>());
                            }

                            platformVersions[platform].Add(calculatedVersion);
                        }
                    }

                    if (version.GetType() == typeof(PackageVersion))
                    {
                        // Filter platform specific version
                        if (!isPlatformSpecific)
                        {
                            xUnitFileStringBuilder.AddVersion(calculatedVersion, version.SupportedFrameworks);
                        }

                        buildFileStringBuilder.AddVersion(calculatedVersion, version.SupportedFrameworks, version.SupportedPlatforms);
                    }
                    else
                    {
                        // Filter platform specific version
                        if (!isPlatformSpecific)
                        {
                            xUnitFileStringBuilder.AddVersionWithDependencies(calculatedVersion, GetDependencies(version), version.SupportedFrameworks, version.SupportedPlatforms);
                        }

                        buildFileStringBuilder.AddVersionWithDependencies(calculatedVersion, GetDependencies(version), version.SupportedFrameworks, version.SupportedPlatforms);
                    }
                }
            }

            xUnitFileStringBuilder.EndTestPackage();
            buildFileStringBuilder.EndTestPackage();

            // Generates platform specific entry
            if (platformVersions.Any())
            {
                foreach (var platform in platformVersions)
                {
                    var platformIntegrationKey = $"{packageVersionDefinition.IntegrationName}_{platform.Key}";
                    additionalPlatforms.Add(platformIntegrationKey);

                    xUnitFileStringBuilder.BeginTestPackage(packageVersionDefinition.TestApplicationName, platformIntegrationKey);

                    foreach (var version in platform.Value)
                    {
                        xUnitFileStringBuilder.AddVersion(version, Array.Empty<string>());
                    }

                    xUnitFileStringBuilder.EndTestPackage();
                }
            }
        }

        // Generate map for all properties
        xUnitFileStringBuilder.BuildLookupMap(PackageVersionDefinitions.Definitions, additionalPlatforms);

        xUnitFileStringBuilder.EndClass();
        buildFileStringBuilder.EndClass();

        var xUnitFilePath = Path.Combine(solutionFolder, "test", "IntegrationTests", "LibraryVersions.g.cs");
        var buildFilePath = Path.Combine(solutionFolder, "build", "LibraryVersions.g.cs");

        await File.WriteAllTextAsync(xUnitFilePath, xUnitFileStringBuilder.ToString());
        await File.WriteAllTextAsync(buildFilePath, buildFileStringBuilder.ToString());
    }

    private static string GetSourceFilePathName([CallerFilePath] string? callerFilePath = null)
        => callerFilePath ?? string.Empty;

    private static string EvaluateVersion(string packageName, string version)
        => version == "*"
            ? _packageVersions[packageName]
            : version;

    private static Dictionary<string, string> GetDependencies(PackageVersion version)
    {
        return version.GetType()
            .GetProperties()
            .Where(x => x.CustomAttributes.Any(x => x.AttributeType == typeof(PackageDependency)))
            .ToDictionary(
                k => k.GetCustomAttribute<PackageDependency>()!.VariableName,
                v =>
                {
                    var packageName = v.GetCustomAttribute<PackageDependency>()!.PackageName;
                    var packageVersion = (string)v.GetValue(version)!;

                    return EvaluateVersion(packageName, packageVersion);
                });
    }
}
