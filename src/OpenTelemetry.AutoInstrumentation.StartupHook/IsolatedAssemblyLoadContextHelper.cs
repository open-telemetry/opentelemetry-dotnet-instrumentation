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

        // 1. Early exit: assembly must have a name
        if (assemblyName.Name is null)
        {
            return null;
        }

        // TODO: caching - .NET has an optimization within an ALC and do not call Load() method
        // if a match (same or higher version) is already loaded in current ALC.
        // However, other situations may trigger the Load() for an assembly that is already loaded
        // when a higher version is requested (e.g., programmatic Assembly.Load with an explicit version).
        // In this case caching will help avoiding unnecessary I/O for version check.

        // Find in TPA (customer/runtime assemblies)
        var tpaAssemblyPath = TrustedPlatformAssembliesHelper.GetAssemblyPath(assemblyName.Name);

        // Find in agent assemblies
        var agentAssemblyPath = ManagedProfilerLocationHelper.FindAssembly(assemblyName.Name)?.Path;

        // 2. Early exit: assembly must be in TPA or in our agent files
        var (assemblyPath, assemblyVersion) = PickHigherVersion(tpaAssemblyPath, agentAssemblyPath);
        if (assemblyPath is null)
        {
            // TODO: log debug once logging is safe here.
            // This is unexpected assembly so include assebly name an dversion
            return null;
        }

        // 3. Early exit: selected assembly must satisfy the requested version
        // If the best available version is still lower than requested, skip
        // rather than loading a version that's too old.
        if (assemblyName.Version is not null)
        {
            assemblyVersion ??= AssemblyUtils.GetAssemblyVersionSafe(assemblyPath);
            if (assemblyVersion is not null && assemblyVersion < assemblyName.Version)
            {
                // TODO: log warning once logging is safe here.
                // The warning should include: assembly name, requested version, and best available version.
                return null;
            }

            // Exception: If we can't determine the version, we still attempt to load the assembly.
            // Rationale:
            // - We've already located a valid assembly by name in TPA or in the agent directory
            // - The only uncertainty is its version
            // - Version mismatches in correctly built applications are not typical
            // - Rejecting a valid assembly and falling back to the Default ALC creates a higher risk
            //   of type and state drift in a scenario that is already documented as unsupported
        }

        // 4. Load conflicting assembly into a given ALC
        return context.LoadFromAssemblyPath(assemblyPath);
    }

    private static (string? Path, Version? Version) PickHigherVersion(string? tpaPath, string? agentPath)
    {
        if (agentPath is null)
        {
            return (tpaPath, null);
        }

        if (tpaPath is null)
        {
            return (agentPath, null);
        }

        var tpaVersion = AssemblyUtils.GetAssemblyVersionSafe(tpaPath);
        var agentVersion = AssemblyUtils.GetAssemblyVersionSafe(agentPath);

        if (tpaVersion is null || agentVersion is null)
        {
            // On error reading version prefer TPA
            return (tpaPath, tpaVersion);
        }

        // TODO we should also check the file version when the assembly versions are the same
        //  e.g. System.Diagnostics.DiagnosticSource from package 10.0.0 and 10.0.2 have the same assembly version

        // Agent wins ONLY if strictly higher
        return agentVersion > tpaVersion ? (agentPath, agentVersion) : (tpaPath, tpaVersion);
    }
}
