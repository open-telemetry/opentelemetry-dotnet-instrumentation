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

        if (assemblyName.Name is null)
        {
            logger.Debug($"Skip loading assembly with null name");
            return null;
        }

        // TODO we may want to cache assembly paths for assemblies loading to custom ALC to avoid repeated file system calls
        // on every resolution, or simply check if the assembly is already loaded into custom ALC
        var assemblyPath = ManagedProfilerLocationHelper.GetAssemblyPath(assemblyName.Name, logger);
        if (assemblyPath is null)
        {
            logger.Debug($"Skip loading unexpected assembly: ({assemblyName})");
            return null;
        }

        // Version check: verify this is a request we should satisfy.
        // See ASSEMBLY RESOLUTION STRATEGY comment in AssemblyResolver.RegisterAssemblyResolving for details.
        if (assemblyName.Version != null)
        {
            try
            {
                var ourVersion = AssemblyUtils.GetAssemblyVersionSafe(assemblyPath);
                if (ourVersion != null && ourVersion < assemblyName.Version)
                {
                    logger.Debug($"Skip loading assembly ({assemblyName}): requested version {assemblyName.Version} is higher than our version {ourVersion}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                logger.Debug($"Failed to read version from \"{assemblyPath}\", proceeding with load: {ex.Message}");
            }
        }

        // Load conflicting library into this ALC for isolation.
        // Unlike the Default.Resolving handler (which fires only after the runtime already failed to satisfy
        // a redirected reference), this Load method fires first — before any TPA fallback.
        // We therefore must check whether our version actually exceeds the TPA version before taking ownership;
        // if TPA has the same or higher version, we return null and let the runtime load from TPA into Default ALC.
        if (AssemblyResolver.TrustedPlatformAssemblyNames.Contains(assemblyName.Name))
        {
            var ourVersion = AssemblyUtils.GetAssemblyVersionSafe(assemblyPath);
            var tpaVersion = AssemblyUtils.GetAssemblyVersionSafe(TrustedPlatformAssembliesHelper.TpaPaths.Single(it => Path.GetFileNameWithoutExtension(it) == assemblyName.Name));
            if (ourVersion > tpaVersion)
            {
                logger.Debug($"Loading \"{assemblyPath}\" with LoadFromAssemblyPath");
                return LoadFromAssemblyPath(assemblyPath);
            }

            logger.Debug($"Skip loading assembly ({assemblyName}): our version {ourVersion} is not higher than TPA version {tpaVersion}");
            return null;
        }

        // else load into default ALC
        // TODO do I need to load it here or just return null?
        logger.Debug($"Loading \"{assemblyPath}\" with AssemblyLoadContext.Default.LoadFromAssemblyPath");
        return Default.LoadFromAssemblyPath(assemblyPath);
    }
}
#endif
