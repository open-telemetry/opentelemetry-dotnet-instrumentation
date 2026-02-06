// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;

namespace OpenTelemetry.AutoInstrumentation.Loader;

/// <summary>
/// Custom AssemblyLoadContext for isolated mode.
/// Loads both customer and agent assemblies, picking higher versions.
/// </summary>
internal class ManagedProfilerAssemblyLoadContext(string managedProfilerDirectory)
    : AssemblyLoadContext("OpenTelemetry.AutoInstrumentation.Loader.ManagedProfilerAssemblyLoadContext", isCollectible: false)
{
    // TODO we may want to define variables for Exlude and Include list (exclude supplements to default excludes, includes overrides)
    // TODO and we can automtically add excludes if an assembly fails to load to custom ALC so it can be loaded to default ALC
    private static readonly HashSet<string> MustUseDefaultAlc = new(StringComparer.OrdinalIgnoreCase) { "System.Private.CoreLib" };

    private static readonly string RuntimeVersionFolder = $"net{Environment.Version.Major}.{Environment.Version.Minor}";

    private readonly Dictionary<string, string> _tpaAssemblies = ParseTrustedPlatformAssemblies();

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // TODO: temporary no logging here! Logging triggers assembly loads -> infinite recursion.

        var name = assemblyName.Name;
        if (string.IsNullOrEmpty(name) || MustUseDefaultAlc.Contains(name))
        {
            return null;
        }

        // Already loaded in this context?
        var cached = Assemblies.FirstOrDefault(a => a.GetName().Name == name);
        if (cached != null)
        {
            return cached;
        }

        // Find in TPA (customer/runtime assemblies)
        _tpaAssemblies.TryGetValue(name, out var tpaPath);

        // Find in agent assemblies (using same logic as AssemblyResolver)
        TryFindManagedProfilerAssemblyPath(name, out var managedProfilerPath);

        // Pick higher version (agent wins only if strictly higher)
        var selected = PickHigherVersion(tpaPath, managedProfilerPath);

        return selected != null ? LoadFromAssemblyPath(selected) : null;
    }

    private static Dictionary<string, string> ParseTrustedPlatformAssemblies()
    {
        try
        {
            return (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string)?.Split(Path.PathSeparator)
                .Where(it => !string.IsNullOrEmpty(it))
                .Select(it => new { FileName = Path.GetFileNameWithoutExtension(it), Path = it })
                .Where(it => !string.IsNullOrEmpty(it.FileName))
                .DistinctBy(it => it.FileName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(it => it.FileName, it => it.Path, StringComparer.OrdinalIgnoreCase)
                ?? [];
        }
        catch
        {
            // Ignore errors parsing TPA
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string? PickHigherVersion(string? tpaPath, string? managedProfilerPath)
    {
        if (managedProfilerPath == null)
        {
            return tpaPath;
        }

        if (tpaPath == null)
        {
            return managedProfilerPath;
        }

        try
        {
            var tpaVersion = AssemblyName.GetAssemblyName(tpaPath).Version;
            var agentVersion = AssemblyName.GetAssemblyName(managedProfilerPath).Version;

            // Agent wins ONLY if strictly higher
            return agentVersion > tpaVersion ? managedProfilerPath : tpaPath;
        }
        catch
        {
            return tpaPath; // On error, prefer TPA
        }
    }

    /// <summary>
    /// Searches for assembly in agent directory using the same logic as AssemblyResolver:
    /// 1. Runtime-specific folder (e.g., net8.0/)
    /// 2. Link file redirect (.dll.link)
    /// 3. Root folder fallback
    /// </summary>
    // TODO same code as in AssemblyResolver.Net - should be optimized
    private bool TryFindManagedProfilerAssemblyPath(string assemblyName, [NotNullWhen(true)] out string? assemblyPath)
    {
        // 1. Runtime-specific path: {agentPath}/net8.0/{name}.dll
        var runtimeSpecificPath = Path.Combine(managedProfilerDirectory, RuntimeVersionFolder, $"{assemblyName}.dll");
        if (File.Exists(runtimeSpecificPath))
        {
            assemblyPath = runtimeSpecificPath;
            return true;
        }

        // 2. Check for .link file: {agentPath}/net8.0/{name}.dll.link
        var linkFilePath = Path.Combine(managedProfilerDirectory, RuntimeVersionFolder, $"{assemblyName}.dll.link");
        if (File.Exists(linkFilePath))
        {
            try
            {
                var targetRuntimeFolder = File.ReadAllText(linkFilePath).Trim();
                var linkedPath = Path.Combine(managedProfilerDirectory, targetRuntimeFolder, $"{assemblyName}.dll");
                if (File.Exists(linkedPath))
                {
                    assemblyPath = linkedPath;
                    return true;
                }
            }
            catch
            {
                // Ignore link file read errors
            }
        }

        // 3. Root folder fallback: {agentPath}/{name}.dll
        var rootPath = Path.Combine(managedProfilerDirectory, $"{assemblyName}.dll");
        if (File.Exists(rootPath))
        {
            assemblyPath = rootPath;
            return true;
        }

        assemblyPath = null;
        return false;
    }
}
#endif
