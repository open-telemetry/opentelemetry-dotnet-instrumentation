using System.Collections.ObjectModel;
using System.Text.Json.Nodes;
using Nuke.Common.IO;

namespace Extensions;

internal static class DepsJsonExtensions
{
    public static string GetFolderRuntimeName(this JsonObject depsJson)
    {
        var runtimeName = depsJson["runtimeTarget"]["name"].GetValue<string>();
        var folderRuntimeName = runtimeName switch
        {
            ".NETCoreApp,Version=v8.0" => "net8.0",
            ".NETCoreApp,Version=v9.0" => "net9.0",
            ".NETCoreApp,Version=v10.0" => "net10.0",
            _ => throw new ArgumentOutOfRangeException(nameof(runtimeName), runtimeName,
                "This value is not supported. You have probably introduced new .NET version to AutoInstrumentation")
        };

        return folderRuntimeName;
    }

    public static void CopyNativeDependenciesToStore(this JsonObject depsJson, AbsolutePath file, IReadOnlyList<AbsolutePath> architectureStores)
    {
        var depsDirectory = file.Parent;

        foreach (var targetProperty in depsJson["targets"].AsObject())
        {
            var target = targetProperty.Value.AsObject();

            foreach (var packages in target)
            {
                if (!packages.Value.AsObject().TryGetPropertyValue("runtimeTargets", out var runtimeTargets))
                {
                    continue;
                }

                foreach (var runtimeDependency in runtimeTargets.AsObject())
                {
                    var sourceFileName = Path.Combine(depsDirectory, runtimeDependency.Key);

                    foreach (var architectureStore in architectureStores)
                    {
                        var targetFileName = Path.Combine(architectureStore, packages.Key.ToLowerInvariant(), runtimeDependency.Key);
                        var targetDirectory = Path.GetDirectoryName(targetFileName);
                        Directory.CreateDirectory(targetDirectory);
                        File.Copy(sourceFileName, targetFileName);
                    }
                }
            }
        }
    }

    public static void RemoveOpenTelemetryLibraries(this JsonObject depsJson)
    {
        var dependencies = depsJson.GetDependencies();
        var runtimeLibraries = depsJson["libraries"].AsObject();
        var keysToRemove = dependencies
            .Where(x => x.Key.StartsWith("OpenTelemetry"))
            .Select(x => x.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            dependencies.Remove(key);
            runtimeLibraries.Remove(key);
        }
    }

    public static void RollFrameworkForward(this JsonObject depsJson, string runtime, string rollForwardRuntime, IReadOnlyList<AbsolutePath> architectureStores)
    {
        // Update the contents of the json file.
        foreach (var dep in depsJson.GetDependencies())
        {
            var depObject = dep.Value.AsObject();
            if (depObject.TryGetPropertyValue("runtime", out var runtimeNode))
            {
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
                    runtimeObject.Add(newKey, libNode);
                }
            }
        }

        // Roll forward each architecture by renaming the tfm folder holding the assemblies.
        foreach (var architectureStore in architectureStores)
        {
            var assemblyDirectories = architectureStore.GetDirectories();
            foreach (var assemblyDirectory in assemblyDirectories)
            {
                var assemblyVersionDirectories = assemblyDirectory.GetDirectories().ToList();
                if (assemblyVersionDirectories.Count != 1)
                {
                    throw new InvalidOperationException(
                        $"Expected exactly one directory under {assemblyDirectory} but found {assemblyVersionDirectories.Count} instead.");
                }

                var assemblyVersionDirectory = assemblyVersionDirectories[0];
                var sourceDir = assemblyVersionDirectory / "lib" / runtime;
                if (sourceDir.Exists())
                {
                    var destDir = assemblyVersionDirectory / "lib" / rollForwardRuntime;

                    sourceDir.Copy(destDir);

                    // Since the json was also rolled forward the original tfm folder can be deleted.
                    sourceDir.DeleteDirectory();
                }
            }
        }
    }

    public static void RemoveDuplicatedLibraries(this JsonObject depsJson, ReadOnlyCollection<AbsolutePath> architectureStores)
    {
        var duplicatedLibraries = new List<(string Name, string Version)> { ("Microsoft.Extensions.Configuration.Binder", "8.0.0") };

        foreach (var duplicatedLibrary in duplicatedLibraries)
        {
            if ((depsJson["libraries"] as JsonObject)!.ContainsKey(duplicatedLibrary.Name + "/" + duplicatedLibrary.Version))
            {
                throw new NotSupportedException($"Cannot remove {duplicatedLibrary.Name.ToLower()}/{duplicatedLibrary.Version} folder. It is referenced in json file");
            }
            foreach (var architectureStore in architectureStores)
            {
                var directoryToBeRemoved = architectureStore / duplicatedLibrary.Name.ToLower() / duplicatedLibrary.Version;
                if (!Directory.Exists(directoryToBeRemoved))
                {
                    throw new NotSupportedException($"Directory {directoryToBeRemoved} does not exists. Verify it.");
                }
                Directory.Delete(directoryToBeRemoved, true);
            }
        }
    }

    private static JsonObject GetDependencies(this JsonObject depsJson)
    {
        return depsJson["targets"].AsObject().First().Value.AsObject();
    }
}
