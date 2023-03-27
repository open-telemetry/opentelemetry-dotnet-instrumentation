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
        var runtime = depsJson.GetTargetFramework();
        return NuGetFramework.Parse(runtime).ToString();
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
        var framework = depsJson.GetTargetFramework();

        // Analyze matching dependencies between adapter package and supported bytecode instrumentation version range
        var dependenciesAnalysisResult = AnalyzeAdapterDependencies(
            // TODO: Scan these packages from OpenTelemetry.AutoInstrumentation.AdditionalDeps
            await NugetPackageHelper.GetPackageDependenciesAsync(framework, MongoClientIntegrationMetadata.AdapterAssembly, "1.3.0"),
            await NugetPackageHelper.GetPackageDependenciesAsync(
                framework,
                MongoClientIntegrationMetadata.InstrumentedAssembly,
                MongoClientIntegrationMetadata.MinimumVersion,
                MongoClientIntegrationMetadata.MaximumVersion)
        );

        // Get common packages across versions
        var commonPackages = GetCommonPackages(dependenciesAnalysisResult);

        foreach (var package in commonPackages)
        {
            // Remove the main library
            RemoveLibrary(depsJson, lib => lib.Equals(package));

            // Remove transient leftovers
            DependencyAnalyzer.Cleanup(map, package)
                .ForEach(transient =>
                    RemoveLibrary(depsJson, lib => lib.Equals(transient)));

            // TODO: This is just cleaning up deps json, we need to manually
            // clean shared store also
        }
    }

    private static IReadOnlySet<string> GetCommonPackages(Dictionary<NuGetVersion, ICollection<string>> analysisResult)
    {
        var commonPackages = new HashSet<string>(analysisResult.First().Value);

        foreach (var item in analysisResult)
        {
            commonPackages.IntersectWith(item.Value);
        }

        return commonPackages;
    }

    private static Dictionary<NuGetVersion, ICollection<string>> AnalyzeAdapterDependencies(
        NuGetPackageInfo adapterPackage,
        IDictionary<NuGetVersion, NuGetPackageInfo> instrumentationPackages)
    {
        var result = new Dictionary<NuGetVersion, ICollection<string>>();

        foreach (var instrumentation in instrumentationPackages)
        {
            var instrumentationVersion = instrumentation.Key;
            var instrumentationPackage = instrumentation.Value;

            if (!result.TryGetValue(instrumentationVersion, out var satisfiedDependencies))
            {
                satisfiedDependencies = new List<string>();
                result[instrumentationVersion] = satisfiedDependencies;
            }

            foreach (var adapterDependency in adapterPackage.Dependencies)
            {
                // Check if instrumentation package dependencies has adapter package dependency
                if (!instrumentationPackage.Dependencies.TryGetValue(adapterDependency.Key, out var dependency))
                {
                    // Adapter dependency is not used by the instrumented assembly dependencies.
                    continue;
                }

                var isDependencyVersionSatisfied = adapterDependency.Value.VersionRange.Satisfies(dependency.VersionRange.MinVersion);
                if (isDependencyVersionSatisfied)
                {
                    satisfiedDependencies.Add(dependency.Id);
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

    public static string GetTargetFramework(this JsonObject depsJson)
    {
        return depsJson["runtimeTarget"]["name"].GetValue<string>();
    }
}
