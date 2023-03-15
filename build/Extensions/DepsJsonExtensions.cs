using System.Text.Json.Nodes;
using Helpers;
using Models;
using NuGet.Frameworks;
using NuGet.Versioning;
using Nuke.Common.IO;
using Nuke.Common.Utilities.Collections;
using OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDb;
using static Nuke.Common.IO.FileSystemTasks;

namespace Extensions;

internal static class DepsJsonExtensions
{
    public static string GetFolderRuntimeName(this JsonObject depsJson)
    {
        var runtimeName = depsJson["runtimeTarget"]["name"].GetValue<string>();
        var folderRuntimeName = runtimeName switch
        {
            ".NETCoreApp,Version=v6.0" => "net6.0",
            ".NETCoreApp,Version=v7.0" => "net7.0",
            _ => throw new ArgumentOutOfRangeException(nameof(runtimeName), runtimeName,
                "This value is not supported. You have probably introduced new .NET version to AutoInstrumentation")
        };

        return folderRuntimeName;
    }

    public static void CopyNativeDependenciesToStore(this JsonObject depsJson, AbsolutePath file, IReadOnlyList<string> architectureStores)
    {
        var depsDirectory = file.Parent;

        foreach (var targetProperty in depsJson["targets"].AsObject())
        {
            var target = targetProperty.Value.AsObject();

            foreach (var packagesNode in target)
            {
                if (!packagesNode.Value.AsObject().TryGetPropertyValue("runtimeTargets", out var runtimeTargets))
                {
                    continue;
                }

                foreach (var runtimeDependency in runtimeTargets.AsObject())
                {
                    var sourceFileName = Path.Combine(depsDirectory, runtimeDependency.Key);

                    foreach (var architectureStore in architectureStores)
                    {
                        var targetFileName = Path.Combine(architectureStore, packagesNode.Key.ToLowerInvariant(), runtimeDependency.Key);
                        var targetDirectory = Path.GetDirectoryName(targetFileName);
                        Directory.CreateDirectory(targetDirectory);
                        File.Copy(sourceFileName, targetFileName);
                    }
                }
            }
        }
    }

    public static void RemoveLibrary(this JsonObject depsJson, Predicate<string> predicate)
    {
        var dependencies = depsJson.GetDependencies();
        var runtimeLibraries = depsJson["libraries"].AsObject();
        var keysToRemove = dependencies
            .Where(x => predicate(x.Key.Split('/')[0]))
            .Select(x => x.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            dependencies.Remove(key);
            runtimeLibraries.Remove(key);
        }
    }

    public static void RemoveOpenTelemetryLibraries(this JsonObject depsJson)
    {
        RemoveLibrary(depsJson, lib => lib.StartsWith("OpenTelemetry", StringComparison.Ordinal));
    }

    public static async Task CleanDuplicatesAsync(this JsonObject depsJson)
    {
        var map = DependencyAnalyzer.BuildDependencyMap(depsJson);

        var result = AnalyzeAdapterDependencies(
            // TODO: Scan these packages from OpenTelemetry.AutoInstrumentation.AdditionalDeps
            await NugetPackageHelper.GetPackageDependenciesAsync(MongoClientIntegrationMetadata.AdapterAssembly, "1.3.0"),
            await NugetPackageHelper.GetPackageDependenciesAsync(
                MongoClientIntegrationMetadata.InstrumentedAssembly,
                MongoClientIntegrationMetadata.MinimumVersion,
                MongoClientIntegrationMetadata.MaximumVersion)
        );

        foreach (var framework in result)
        {
            var latestVersion = framework.Value
                .OrderByDescending(x => x.Key.Version)
                .FirstOrDefault();

            if (latestVersion.Value == null)
            {
                continue;
            }

            foreach (var dependency in latestVersion.Value)
            {
                // All package versions contain the same optimisable dependency
                var canBeOptimized = framework.Value
                    .All(x => x.Value.Contains(dependency));
                if (!canBeOptimized)
                {
                    continue;
                }

                // Remove the main library
                RemoveLibrary(depsJson, lib => lib.Equals(dependency));

                // Remove transient leftovers
                DependencyAnalyzer.Cleanup(map, dependency)
                    .ForEach(transient =>
                        RemoveLibrary(depsJson, lib => lib.Equals(transient)));

                // TODO: This is just cleaning up deps json, we need to manually
                // clean shared store also?
            }
        }
    }

    private static Dictionary<NuGetFramework, Dictionary<NuGetVersion, ICollection<string>>> AnalyzeAdapterDependencies(
        PackageDependencySets adapterPackage,
        IEnumerable<(NuGetVersion Version, PackageDependencySets DependencySets)> instrumentationPackages)
    {
        var result = new Dictionary<NuGetFramework, Dictionary<NuGetVersion, ICollection<string>>>();

        // adapter packages part
        foreach (var adapterDependencyGroup in adapterPackage)
        {
            var framework = adapterDependencyGroup.Key;

            result.TryAdd(framework, new Dictionary<NuGetVersion, ICollection<string>>());

            foreach (var adapterDependency in adapterDependencyGroup.Value)
            {
                // Instrumentation packages part
                foreach (var instrumentationPackage in instrumentationPackages)
                {
                    var dependencyGroup = result[framework];
                    dependencyGroup.TryAdd(instrumentationPackage.Version, new List<string>());

                    var hasCommonDependency = instrumentationPackage
                        .DependencySets[framework]
                        .ContainsKey(adapterDependency.Value.Id);
                    if (!hasCommonDependency)
                    {
                        continue;
                    }

                    var dependency = instrumentationPackage.DependencySets[framework][adapterDependency.Value.Id];
                    var isDependencyVersionSatisfied = adapterDependency.Value.VersionRange.Satisfies(dependency.VersionRange.MinVersion);
                    if (isDependencyVersionSatisfied)
                    {
                        dependencyGroup[instrumentationPackage.Version].Add(dependency.Id);
                    }
                }
            }
        }

        return result;
    }

    public static void RollFrameworkForward(this JsonObject depsJson, string runtime, string rollForwardRuntime, IReadOnlyList<string> architectureStores)
    {
        // Update the contents of the json file.
        foreach (var dep in depsJson.GetDependencies())
        {
            var depObject = dep.Value.AsObject();
            if (!depObject.TryGetPropertyValue("runtime", out var runtimeNode))
            {
                continue;
            }

            var runtimeObject = runtimeNode.AsObject();
            var libKeys = runtimeObject
                .Select(x => x.Key)
                .Where(x => x.StartsWith($"lib/{runtime}"))
                .ToList();

            foreach (var libKey in libKeys)
            {
                var libNode = runtimeObject[libKey];
                var newKey = libKey.Replace($"lib/{runtime}", $"lib/{rollForwardRuntime}");

                runtimeObject.Remove(libKey);
                runtimeObject.AddPair(newKey, libNode);
            }
        }

        // Roll forward each architecture by renaming the tfm folder holding the assemblies.
        foreach (var architectureStore in architectureStores)
        {
            var assemblyDirectories = Directory.GetDirectories(architectureStore);
            foreach (var assemblyDirectory in assemblyDirectories)
            {
                var assemblyVersionDirectories = Directory.GetDirectories(assemblyDirectory);
                if (assemblyVersionDirectories.Length != 1)
                {
                    throw new InvalidOperationException(
                        $"Expected exactly one directory under {assemblyDirectory} but found {assemblyVersionDirectories.Length} instead.");
                }

                var assemblyVersionDirectory = assemblyVersionDirectories[0];
                var sourceDir = Path.Combine(assemblyVersionDirectory, "lib", runtime);
                if (Directory.Exists(sourceDir))
                {
                    var destDir = Path.Combine(assemblyVersionDirectory, "lib", rollForwardRuntime);

                    CopyDirectoryRecursively(sourceDir, destDir);

                    // Since the json was also rolled forward the original tfm folder can be deleted.
                    DeleteDirectory(sourceDir);
                }
            }
        }
    }

    public static JsonObject GetDependencies(this JsonObject depsJson)
    {
        return depsJson["targets"].AsObject().First().Value.AsObject();
    }
}
