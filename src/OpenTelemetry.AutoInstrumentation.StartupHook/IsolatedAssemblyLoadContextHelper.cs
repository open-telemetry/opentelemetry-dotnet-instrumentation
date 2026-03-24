// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using System.Runtime.Loader;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation;

/// <summary>
/// Custom AssemblyLoadContext Helper for isolated mode.
/// Loads both customer and agent assemblies, picking higher versions,
/// but skipping if the best available version is lower than the requested version.
/// </summary>
internal static class IsolatedAssemblyLoadContextHelper
{
    public static Assembly? Load(AssemblyLoadContext context, AssemblyName assemblyName)
    {
        // TODO: temporary no logging here! Logging triggers assembly loads -> infinite recursion.

        var name = assemblyName.Name!;

        // TODO: caching - .NET has an optimization within an ALC and do not call Load() method
        // if a match (same or higher version) is already loaded in current ALC.
        // However, other situations may trigger the Load() for an assembly that is already loaded
        // when a higher version is requested (e.g., programmatic Assembly.Load with an explicit version).
        // In this case caching will help avoiding unnecessary I/O for version check.

        // Find in TPA (customer/runtime assemblies)
        var tpaPath = TrustedPlatformAssembliesHelper.GetAssemblyPath(name);

        // Find in agent assemblies
        var agentPath = ManagedProfilerLocationHelper.GetAssemblyPath(name);

        // StartupHook.SaveAssemblyNames($"Isolated-{assemblyName.Name}+{tpaPath is not null}+{agentPath is not null}");

        // Pick higher version (agent wins only if strictly higher)
        var selected = PickHigherVersion(tpaPath, agentPath);
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
                var selectedVersion = AssemblyUtils.GetAssemblyVersionSafe(selected);
                if (selectedVersion != null && selectedVersion < assemblyName.Version)
                {
                    // TODO: log warning once logging is safe here.
                    // The warning should include: assembly name, requested version, and best available version.
                    return null;
                }
            }
            catch
            {
                // On error reading version, fall through and attempt to load:
                // by now we have the assembly, located by name in TPA or the agent directory;
                // the only missing piece is a confirmed version.
                // Version mismatches in a correctly built standalone app are not a typical scenario,
                // so skipping here converts a lower-probability uncertainty into a certain fallback
                // to Default ALC, which carries a higher risk of type and state drift than the alternative
                // of loading an assembly at a potentially mismatched version in a scenario
                // that is already documented as unsupported and not typical to reach.
            }
        }

        return context.LoadFromAssemblyPath(selected);
    }

    private static string? PickHigherVersion(string? tpaPath, string? agentPath)
    {
        if (agentPath is null)
        {
            return tpaPath;
        }

        if (tpaPath is null)
        {
            return agentPath;
        }

        try
        {
            var tpaVersion = AssemblyUtils.GetAssemblyVersionSafe(tpaPath);
            var agentVersion = AssemblyUtils.GetAssemblyVersionSafe(agentPath);

            // TODO we should also check the file version when the assembly versions are the same
            //  e.g. System.Diagnostics.DiagnosticSource from package 10.0.0 and 10.0.2 have the same assembly version

            // Agent wins ONLY if strictly higher
            return agentVersion > tpaVersion ? agentPath : tpaPath;
        }
        catch
        {
            return tpaPath; // On error, prefer TPA
        }
    }
}
