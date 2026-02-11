// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation.Loader;

/// <summary>
/// A class that attempts to load the OpenTelemetry.AutoInstrumentation .NET assembly.
/// </summary>
internal partial class AssemblyResolver
{
    internal static AssemblyLoadContext DependencyLoadContext { get; } = new AssemblyLoadContext("OpenTelemetry.AutoInstrumentation.Loader.AssemblyResolver", false);

    internal static string[] TrustedPlatformAssemblyNames { get; } = GetTrustedPlatformAssemblyNames();

    internal void RegisterAssemblyResolving()
    {
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
        //
        // === NUGET PACKAGE DEPLOYMENT (Need Investigation) ===
        // NuGet resolves versions at build time; TPA typically has correct versions.
        // This handler serves as fallback for edge cases.
        //
        // === STARTUP HOOK ONLY (Not Currently Supported) ===
        // StartupHook lacks:
        //   - Native profiler's IL redirection capabilities
        //   - Build-time version resolution benefits
        // To be implemented in follow-up changes.
        AssemblyLoadContext.Default.Resolving += Resolving_ManagedProfilerDependencies;
    }

    private static string[] GetTrustedPlatformAssemblyNames()
    {
        return [.. TrustedPlatformAssembliesHelper.TpaPaths.Select(Path.GetFileNameWithoutExtension).OfType<string>()];
    }

    private Assembly? Resolving_ManagedProfilerDependencies(AssemblyLoadContext context, AssemblyName assemblyName)
    {
        // TODO do we want to cache this information so we don't need to check and read files every time?
        bool TryFindAssemblyPath(AssemblyName assemblyName, [NotNullWhen(true)] out string? assemblyPath)
        {
            // For .NET (Core) most of the assembblies are different per runtime version so we start first with runtime specific folder
            // _managedProfilerDirectory already contains the version folder (e.g., tracer-home/net/net8.0)
            var runtimeSpecificPath = Path.Combine(_managedProfilerDirectory, $"{assemblyName.Name}.dll");
            if (File.Exists(runtimeSpecificPath))
            {
                assemblyPath = runtimeSpecificPath;
                return true;
            }

            // if assembly is missing it might be linked, so we check for .link file
            var link = Path.Combine(_managedProfilerDirectory, $"{assemblyName.Name}.dll.link");
            if (File.Exists(link))
            {
                try
                {
                    var linkRuntimeVersionFolder = File.ReadAllText(link).Trim();
                    // Get parent directory (tracer-home/net) to combine with link target version folder
                    var linkPath = Path.Combine(Path.GetDirectoryName(_managedProfilerDirectory) ?? _managedProfilerDirectory, linkRuntimeVersionFolder, $"{assemblyName.Name}.dll");
                    if (File.Exists(linkPath))
                    {
                        assemblyPath = linkPath;
                        return true;
                    }
                    else
                    {
                        logger.Error($"Linked assembly path \"{linkPath}\" does not exist");
                        assemblyPath = null;
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    logger.Debug(ex, $"Error reading .link file: \"{link}\"");
                    assemblyPath = null;
                    return false;
                }
            }

            // last we fallback to root managed profiler folder (tracer-home/net)
            var rootPath = Path.Combine(Path.GetDirectoryName(_managedProfilerDirectory) ?? _managedProfilerDirectory, $"{assemblyName.Name}.dll");
            if (File.Exists(rootPath))
            {
                assemblyPath = rootPath;
                return true;
            }

            assemblyPath = null;
            return false;
        }

        logger.Debug($"Check assembly: ({assemblyName})");

        if (!TryFindAssemblyPath(assemblyName, out var assemblyPath))
        {
            logger.Debug($"Skip loading unexpected assembly: ({assemblyName})");
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
