// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using System.Runtime.Loader;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation.Loader;

/// <summary>
/// A class that attempts to load the OpenTelemetry.AutoInstrumentation .NET assembly.
/// </summary>
internal partial class AssemblyResolver(IOtelLogger logger)
{
    internal static AssemblyLoadContext DependencyLoadContext { get; } = new AssemblyLoadContext("OpenTelemetry.AutoInstrumentation.Loader.AssemblyResolver", false);

    internal static string[] TrustedPlatformAssemblyNames { get; } = GetTrustedPlatformAssemblyNames();

    internal void RegisterAssemblyResolving()
    {
        // ASSEMBLY RESOLUTION STRATEGY
        //
        // === NATIVE PROFILER DEPLOYMENT ===
        // The native profiler already redirected (IL rewriting) all references to our versions.
        // The Resolving event fires when runtime cannot find the assembly in two cases:
        //
        // Case 1: Assembly NOT in TrustedPlatformAssembly list (our dependencies; e.g., OpenTelemetry.dll)
        //   -> Runtime has no default location for this assembly
        //   -> Resolving event fires
        //   -> We load to Default AssemblyLoadContext (no version conflict risk)
        //
        // Case 2: Assembly IN TPA with lower version (conflict)
        //   -> Customer's TPA has lower version, profiler redirects to higher version
        //   -> Runtime cannot satisfy higher version due to TPA conflict
        //   -> Resolving event fires
        //   -> We load to Custom ALC for isolation (loading to Default ALC will fail)
        //   -> NOTE: If TPA has same/higher version, runtime successfully auto-loads to Default ALC;
        //            event never fires (accepted)

        // ASSEMBLY RESOLUTION TIMING
        //
        // When the runtime cannot find an assembly, AssemblyLoadContext.Default.Resolving fires before
        // AppDomain.CurrentDomain.AssemblyResolve, so we subscribe to the former for guaranteed control.
        //
        // While we could subscribe to AppDomain.CurrentDomain.AssemblyResolve, the timing of this
        // subscription relative to the built-in handler (Assembly.LoadFromResolveHandler) subscription is
        // unpredictable and fragile to code changes. If the built-in handler runs first, it loads co-located
        // assemblies (our layout - OpenTelemetry.dll library and its dependency System.Diagnostics.DiagnosticSource.dll)
        // into the Default context via Assembly.LoadFrom, causing loading failure if the customer application
        // has the same dependency but to a lower version (in the example above, DiagnosticSource dll)

        AssemblyLoadContext.Default.Resolving += Resolving_ManagedProfilerDependencies;
    }

    private static string[] GetTrustedPlatformAssemblyNames()
    {
        return [.. TrustedPlatformAssembliesHelper.TpaPaths.Select(Path.GetFileNameWithoutExtension).OfType<string>()];
    }

    private Assembly? Resolving_ManagedProfilerDependencies(AssemblyLoadContext context, AssemblyName assemblyName)
    {
        logger.Debug($"Check assembly: ({assemblyName})");

        if (assemblyName.Name is null)
        {
            logger.Debug($"Skip resolving assembly with null name");
            return null;
        }

        // TODO we may want to cache assembly paths for assemblies loading to custom ALC to avoid repeated file system calls on every resolution,
        // or simply check if the assembly is already loaded into custom ALC but that require rewriting the logic a bit
        var assemblyPath = ManagedProfilerLocationHelper.GetAssemblyPath(assemblyName.Name, logger);
        if (assemblyPath is null)
        {
            logger.Debug($"Skip resolving unexpected assembly: ({assemblyName})");
            return null;
        }

        // Load conflicting library into a custom ALC
        if (TrustedPlatformAssemblyNames.Contains(assemblyName.Name))
        {
            logger.Debug($"Loading \"{assemblyPath}\" with DependencyLoadContext.LoadFromAssemblyPath");
            return DependencyLoadContext.LoadFromAssemblyPath(assemblyPath);
        }

        // else load into default ALC
        logger.Debug($"Loading \"{assemblyPath}\" with AssemblyLoadContext.Default.LoadFromAssemblyPath");
        return AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
    }
}
