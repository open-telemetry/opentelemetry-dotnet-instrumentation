// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using System.Reflection;
using System.Runtime.Loader;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation.Loader;

/// <summary>
/// A class that attempts to load the OpenTelemetry.AutoInstrumentation .NET assembly.
/// </summary>
internal class AssemblyResolver(IOtelLogger logger)
{
    internal static HashSet<string> TrustedPlatformAssemblyNames { get; } = new(GetTrustedPlatformAssemblyNames(), StringComparer.OrdinalIgnoreCase);

    internal AssemblyLoadContext DependencyLoadContext { get; } = new ManagedProfilerAssemblyLoadContext(logger);

    internal void RegisterAssemblyResolving()
    {
        // ASSEMBLY RESOLUTION STRATEGY
        //
        // === NATIVE PROFILER DEPLOYMENT ===
        // The native profiler already redirected (IL rewriting) all conflicting AssemblyRef to our versions.
        // In most cases the Resolving event fires because of this redirection and we know exactly
        // what to do - just decide which context to load the assembly into:
        //
        // Case 1: Assembly NOT in TrustedPlatformAssembly list (our dependencies; e.g., OpenTelemetry.dll)
        //   -> Runtime has no knowledge about this assembly
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
        //
        // Note: However, other situations may also trigger the Resolving event for an assembly we ship
        // (e.g., programmatic Assembly.Load with an explicit version that .net runtime cannot satisfy).
        // To avoid accidentally satisfying a request that is not ours or one we cannot fulfill,
        // we validate versions before loading: if our version >= requested, we proceed (backward compatible);
        // if the requested version is higher than ours, we skip the request.

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

        // ASSEMBLY REDIRECTION AND REFLECTION
        //
        // Native Assembly Redirection (IL rewriting) only works for assemblies that are being loaded,
        // so we must ensure that reflection-triggered loads are also intercepted.
        // GetType / Load(AssemblyName) calls bypass IL rewriting entirely —
        // so the assemblies from TPA will be automatically loaded to Default ALC by by-passing Default.Resolving
        // and may potentially cause a drift if our version of the assembly is higher that the TPA.
        // To fix this, we set DependencyLoadContext as the contextual reflection ALC via
        // EnterContextualReflection(), so those calls route through Load(AssemblyName) first
        // and TPA conflicts can be intercepted before the runtime silently loads the wrong version.

        AssemblyLoadContext.Default.Resolving += Resolving_ManagedProfilerDependencies;
        // TODO with setting DependencyLoadContext as contextual reflection context, these become more important:
        //  - need of caching, because for unknown assemblies we will be hitting file system many times
        //  - for unknown assemblies we'll be hitting both DependencyLoadContext.Load and this event handler
        DependencyLoadContext.EnterContextualReflection();
    }

    private static string[] GetTrustedPlatformAssemblyNames()
    {
        return [.. TrustedPlatformAssembliesHelper.TpaPaths.Select(Path.GetFileNameWithoutExtension).OfType<string>()];
    }

    private Assembly? Resolving_ManagedProfilerDependencies(AssemblyLoadContext context, AssemblyName assemblyName)
    {
        logger.Debug($"Check resolving assembly: ({assemblyName})");

        // 1. Early exit: assembly must have a name
        if (assemblyName.Name is null)
        {
            logger.Debug($"Skip resolving assembly with null name");
            return null;
        }

        // TODO we may want to cache assembly paths for assemblies loading to custom ALC to avoid repeated file system calls
        // on every resolution, or simply check if the assembly is already loaded into custom ALC

        // 2. Early exit: assembly must be in our agent files
        var assemblyPath = ManagedProfilerLocationHelper.GetAssemblyPath(assemblyName.Name, logger);
        if (assemblyPath is null)
        {
            logger.Debug("Skip resolving unexpected assembly");
            return null;
        }

        // 3. Early exit: our assembly must satisfy the requested version
        // See ASSEMBLY RESOLUTION STRATEGY Note comment in RegisterAssemblyResolving for details.
        if (assemblyName.Version is not null)
        {
            var assemblyVersion = AssemblyUtils.GetAssemblyVersionSafe(assemblyPath);
            if (assemblyVersion is not null && assemblyVersion < assemblyName.Version)
            {
                logger.Debug($"Skip resolving assembly ({assemblyName}): requested version [v{assemblyName.Version}] is higher than our version [v{assemblyVersion}]");
                return null;
            }

            // Exception: If we can't determine the version, we still attempt to load the assembly.
            // Rationale:
            // - We've already located assembly by name in the agent directory
            // - The only uncertainty is the version
            // - Version mismatches with native AssemblyRef redirection are not typical
            // - Rejecting a valid agent dependency creates a higher risk of
            //     1) loading an assembly from TPA at a lower version or
            //     2) not loading the agent dependency at all,
            //   in a scenario that is already documented as unsupported
            if (assemblyVersion is null)
            {
                logger.Debug($"Failed to read version from \"{assemblyPath}\", proceeding with resolving");
            }
        }

        // 4. Load conflicting assembly into custom ALC, otherwise into Default ALC
        var loadContext = TrustedPlatformAssemblyNames.Contains(assemblyName.Name)
            ? DependencyLoadContext
            : AssemblyLoadContext.Default;
        logger.Debug($"Loading \"{assemblyPath}\" with {loadContext.Name}.LoadFromAssemblyPath");
        return loadContext.LoadFromAssemblyPath(assemblyPath);
    }
}

#endif
