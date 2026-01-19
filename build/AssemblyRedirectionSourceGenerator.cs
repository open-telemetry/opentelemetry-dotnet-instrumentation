using System.Text;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Serilog;

public static class AssemblyRedirectionSourceGenerator
{
    public static void Generate(string assembliesFolderPath, string generatedFilePath)
    {
        Log.Debug("Generating assembly redirection file {0}", generatedFilePath);
        var assemblies = new SortedDictionary<int, SortedDictionary<string, AssemblyNameDefinition>>();

        // Process both netfx and net folders with their specific regex patterns for version extraction
        // TODO for now generate headers files with 3 digit version numbers for net framework and 4+ digit version numbers for net (core)
        // e.g. net462 -> 462, net47 -> 470, net8.0 -> 8000, net10.0 -> 10000
        // TODO rename header file to more general name since it will contain both netfx and net redirections
        var rootFolders = new[]
        {
            (Name: "netfx", Pattern: new Regex(@"^net(?<version>\d{2,3})$")),  // .NET Framework: net462, net47, net471, net472
            (Name: "net", Pattern: new Regex(@"^net(?<version>\d{1,2}\.\d)$"))  // .NET (Core): net8.0, net9.0, net10.0
        };

        foreach (var (rootFolderName, frameworkVersionRegEx) in rootFolders)
        {
            var rootFolderPath = Path.Combine(assembliesFolderPath, rootFolderName);
            if (!Directory.Exists(rootFolderPath))
            {
                Log.Warning("Root folder {0} does not exist, skipping", rootFolderPath);
                continue;
            }

            var frameworkFolders = new Dictionary<int, string>();

            // Discover framework-specific subfolders
            foreach (var directory in Directory.EnumerateDirectories(rootFolderPath))
            {
                var folderName = Path.GetFileName(directory);
                var framework = frameworkVersionRegEx.Match(folderName).Groups["version"].Value;
                if (framework == string.Empty)
                {
                    Log.Error("Unexpected folder name: {0}, folder \"{1}\" will not be processed", framework, directory);
                    continue;
                }

                var frameworkVersion = int.Parse(framework.Replace(".", ""));
                // .NET (Core) versions should go higher than .NET Framework versions
                if (framework.Contains('.'))
                {
                    frameworkVersion *= 100;
                }
                if (frameworkVersion < 100)
                {
                    frameworkVersion *= 10;
                }

                if (frameworkFolders.TryGetValue(frameworkVersion, out var folder))
                {
                    Log.Error("For {0}: already registered folder {1}, {2} will be skipped", frameworkVersion, folder, directory);
                    continue;
                }
                frameworkFolders[frameworkVersion] = directory;
                assemblies[frameworkVersion] = new SortedDictionary<string, AssemblyNameDefinition>();
            }

            void Process(string fileName, int? framework)
            {
                try
                {
                    using var moduleDef = ModuleDefinition.ReadModule(fileName);
                    var assemblyDef = moduleDef.Assembly.Name!;
                    if (assemblyDef.Name == "netstandard")
                    {
                        // Skip netstandard, since it doesn't need redirection.
                        return;
                    }

                    foreach (var keys in framework != null ? (IEnumerable<int>)[framework.Value] : frameworkFolders.Keys)
                    {
                        assemblies[keys][assemblyDef.Name] = assemblyDef;
                        Log.Debug("Adding {0} assembly to the redirection map {1}. Targeted version {2}", assemblyDef.Name,
                            keys, assemblyDef.Version);
                    }
                }
                catch (BadImageFormatException)
                {
                    Log.Debug("Skipping \"{0}\" couldn't open it as a managed assembly", fileName);
                }
            }

            // Process common assemblies in root folder
            foreach (var fileName in Directory.EnumerateFiles(rootFolderPath))
            {
                Process(fileName, null);
            }

            // Process framework-specific assemblies
            foreach (var fx in frameworkFolders)
            {
                foreach (var fileName in Directory.EnumerateFiles(fx.Value))
                {
                    var filenameToProcess = fileName;
                    if (Path.GetExtension(fileName) == ".link")
                    {
                        filenameToProcess = Path.Combine(fx.Value, "..", File.ReadAllText(fileName),
                            Path.GetFileNameWithoutExtension(fileName));
                    }

                    Process(filenameToProcess, fx.Key);
                }
            }
        }

        var sourceContents = GenerateSourceContents(assemblies);

        File.WriteAllText(generatedFilePath, sourceContents);
        Log.Information("Assembly redirection source generated {0}", generatedFilePath);
    }

    private static string GenerateSourceContents(SortedDictionary<int, SortedDictionary<string, AssemblyNameDefinition>> assemblies)
    {
        #pragma warning disable format
        return
        $$"""
        /*
         * Copyright The OpenTelemetry Authors
         * SPDX-License-Identifier: Apache-2.0
         */

        // Auto-generated file, do not change it - generated by the {{nameof(AssemblyRedirectionSourceGenerator)}} type

        #include "cor_profiler.h"

        #define STR(Z1) #Z1
        #define AUTO_MAJOR STR(OTEL_AUTO_VERSION_MAJOR) 
        
        namespace trace
        {
        void CorProfiler::InitNetFxAssemblyRedirectsMap()
        {
            const USHORT auto_major = atoi(AUTO_MAJOR);

            assembly_version_redirect_map_.insert({
                {{GenerateEntries(assemblies)}}
            });
        }
        }

        """;
        #pragma warning restore format
    }

    private static string GenerateEntries(SortedDictionary<int, SortedDictionary<string, AssemblyNameDefinition>> frameworks)
    {
        var sb = new StringBuilder();

        foreach (var fx in frameworks)
        {
            sb.AppendLine($"        {{ {fx.Key}, {{");
            foreach (var kvp in fx.Value)
            {
                var v = kvp.Value.Version!;
                if (kvp.Key != "OpenTelemetry.AutoInstrumentation")
                {
                    sb.AppendLine($"            {{ L\"{kvp.Key}\", {{{v.Major}, {v.Minor}, {v.Build}, {v.Revision}}} }},");
                }
                else
                {
                    sb.AppendLine($"            {{ L\"{kvp.Key}\", {{auto_major, 0, 0, 0}} }},");
                }
            }
            sb.AppendLine("        }},");
        }

        return sb.ToString()
            .AsSpan() // Optimisation for following string manipulations
            .Trim() // Remove whitespaces
            .TrimEnd(',') // Remove trailing comma
            .ToString();
    }
}
