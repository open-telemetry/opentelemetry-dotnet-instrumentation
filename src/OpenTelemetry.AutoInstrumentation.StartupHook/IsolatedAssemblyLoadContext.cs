// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using System.Runtime.Loader;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation;

/// <summary>
/// Custom AssemblyLoadContext for isolated mode.
/// Loads both customer and agent assemblies, picking higher versions,
/// but skipping if the best available version is lower than the requested version.
/// </summary>
internal class IsolatedAssemblyLoadContext()
    : AssemblyLoadContext(StartupHookConstants.IsolatedAssemblyLoadContextName, isCollectible: false)
{
    // TODO we may want to define variables for Exlude and Include list (exclude supplements to default excludes, includes overrides)
    // TODO which will give flexibility for the customer if they know what they are doing;
    // TODO also we can automtically add to excludes, if an assembly failed to load in custom ALC so we won't fail it over and over
    private static readonly HashSet<string> MustUseDefaultAlc = new(StringComparer.OrdinalIgnoreCase) { "System.Private.CoreLib" };

    private readonly Dictionary<string, string> _tpaAssemblies = ParseTrustedPlatformAssemblies();

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // TODO: temporary no logging here! Logging triggers assembly loads -> infinite recursion.

        var name = assemblyName.Name;
        if (string.IsNullOrEmpty(name) || MustUseDefaultAlc.Contains(name))
        {
            return null;
        }

        // TODO: caching - the .NET runtime performs name-based unification within an ALC and
        // should not call Load() again for an already-loaded assembly name (Default ALC also never
        // triggers this Load()). But if there is a risk identified where Load() is re-entered
        // for the same name, add a cache check here. Bundle with Loader AssemblyResolver caching work.

        // Find in TPA (customer/runtime assemblies)
        _tpaAssemblies.TryGetValue(name, out var tpaPath);

        // Find in agent assemblies
        var managedProfilerPath = ManagedProfilerLocationHelper.GetAssemblyPath(name);

        // Pick higher version (agent wins only if strictly higher)
        var selected = PickHigherVersion(tpaPath, managedProfilerPath);
        if (selected == null)
        {
            // TODO: log debug once logging is safe here.
            // This is unexpected assembly so include assebly name an dversion
            return null;
        }

        // Verify that the selected assembly satisfies the requested version.
        // If the best available version is still lower than requested, skip
        // rather than loading a version that's too old.
        if (assemblyName.Version != null)
        {
            try
            {
                var selectedVersion = AssemblyName.GetAssemblyName(selected).Version;
                if (selectedVersion != null && selectedVersion < assemblyName.Version)
                {
                    // TODO: log warning once logging is safe here.
                    // The warning should include: assembly name, requested version, and best available version.
                    return null;
                }
            }
            catch
            {
                // On error reading version, fall through and attempt to load
            }
        }

        return LoadFromAssemblyPath(selected);
    }

    private static Dictionary<string, string> ParseTrustedPlatformAssemblies()
    {
        return TrustedPlatformAssembliesHelper.TpaPaths
            .Select(path => new { Name = Path.GetFileNameWithoutExtension(path), Path = path })
            .Where(x => !string.IsNullOrEmpty(x.Name))
            .DistinctBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Name, x => x.Path, StringComparer.OrdinalIgnoreCase);
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
}
