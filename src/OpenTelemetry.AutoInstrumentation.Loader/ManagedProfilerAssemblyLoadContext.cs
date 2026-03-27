// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using System.Reflection;
using System.Runtime.Loader;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation.Loader;

internal class ManagedProfilerAssemblyLoadContext(IOtelLogger logger) : AssemblyLoadContext("OpenTelemetry.AutoInstrumentation.Loader.ManagedProfilerAssemblyLoadContext", false)
{
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        logger.Debug($"Check loading assembly: ({assemblyName})");

        // 1. Early exit: assembly must have a name
        if (assemblyName.Name is null)
        {
            logger.Debug($"Skip loading assembly with null name");
            return null;
        }

        // TODO we may want to cache assembly paths for assemblies loading to custom ALC to avoid repeated file system calls
        // on every resolution, or simply check if the assembly is already loaded into custom ALC

        // 2. Early exit: assembly must be in our agent files
        var agentAssemblyPath = ManagedProfilerLocationHelper.FindAssembly(assemblyName.Name, logger)?.Path;
        if (agentAssemblyPath is null)
        {
            logger.Debug("Skip loading unexpected assembly");
            return null;
        }

        logger.Debug($"Found assembly in agent files: \"{agentAssemblyPath}\"");

        // Unlike the AssemblyResolver.Resolving_ManagedProfilerDependencies event handler
        // which fires only after the runtime already failed to satisfy a redirected reference,
        // this Load method fires first — before any TPA fallback.
        // We therefore must check whether our version actually exceeds the TPA version before taking ownership;
        // if TPA has the same or higher version, we should prefer TPA version loaded into Default ALC.
        var tpaAssemblyPath = TrustedPlatformAssembliesHelper.GetAssemblyPath(assemblyName.Name);

        // 3. Early exit: TPA is best choice - defer to runtime
        var selection = PickHigherVersion(tpaAssemblyPath, agentAssemblyPath);
        if (selection is null)
        {
            logger.Debug($"Skip loading assembly ({assemblyName}): Deferring to runtime for TPA assembly: \"{tpaAssemblyPath}\"");
            return null;
        }

        var (assemblyPath, assemblyVersion, loadContext) = selection.Value;
        logger.Debug($"Selected agent assembly [v{assemblyVersion}] to load into [{loadContext.Name}]: \"{assemblyPath}\"");

        // 4. Early exit: agent assembly must satisfy the requested version
        if (assemblyName.Version is not null)
        {
            assemblyVersion ??= AssemblyUtils.GetAssemblyVersionSafe(assemblyPath);
            if (assemblyVersion is not null && assemblyVersion < assemblyName.Version)
            {
                logger.Debug($"Skip loading assembly ({assemblyName}): requested version [v{assemblyName.Version}] is higher than best available version [v{assemblyVersion}]");
                return null;
            }

            // Exception: If we can't determine the version, we still attempt to load the assembly.
            // Rationale:
            // - We've already located assembly by name in TPA or in the agent directory
            // - The only uncertainty is the version
            // - Version mismatches with native AssemblyRef redirection are not typical
            // - Rejecting a valid assembly creates a higher risk of
            //     1) loading an assembly from TPA at a lower version or
            //     2) not loading the agent dependency at all,
            //   in a scenario that is already documented as unsupported
            if (assemblyVersion is null)
            {
                logger.Debug($"Failed to read version from \"{assemblyPath}\", proceeding with load");
            }
        }

        // 5. Load agent assembly into appropriate ALC
        logger.Debug($"Loading \"{assemblyPath}\" with {loadContext.Name}.LoadFromAssemblyPath");
        return loadContext.LoadFromAssemblyPath(assemblyPath);
    }

    private (string Path, Version? Version, AssemblyLoadContext Context)? PickHigherVersion(string? tpaPath, string agentPath)
    {
        if (tpaPath is null)
        {
            // Agent not in TPA - load agent to Default ALC so app code can access it
            logger.Debug($"Assembly not in TPA");
            return (agentPath, null, Default);
        }

        var tpaVersion = AssemblyUtils.GetAssemblyVersionSafe(tpaPath);
        var agentVersion = AssemblyUtils.GetAssemblyVersionSafe(agentPath);

        if (tpaVersion is null || agentVersion is null)
        {
            // On error reading version, defer to runtime default behavior (TPA)
            logger.Debug($"Failed to read version (TPA: {tpaVersion}, Agent: {agentVersion})");
            return null;
        }

        logger.Debug($"Version comparison: Agent [v{agentVersion}] vs TPA [v{tpaVersion}]");

        // TODO we should also check the file version when the assembly versions are the same
        //  e.g. System.Diagnostics.DiagnosticSource from package 10.0.0 and 10.0.2 have the same assembly version

        // Agent wins ONLY if strictly higher, otherwise defer to runtime default behavior (TPA)
        return agentVersion > tpaVersion ? (agentPath, agentVersion, this) : null;
    }
}
#endif
