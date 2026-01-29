// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

// TODO remove prgama after cleanup
#pragma warning disable CA1303 // Do not pass literals as localized parameters
#if NET
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;

namespace OpenTelemetry.AutoInstrumentation.Loader;

/// <summary>
/// A class that attempts to load the OpenTelemetry.AutoInstrumentation .NET assembly.
/// </summary>
internal partial class AssemblyResolver
{
    internal static AssemblyLoadContext DependencyLoadContext { get; } = new ManagedProfilerAssemblyLoadContext();

    internal static string[] TrustedPlatformAssemblyNames { get; } = GetTrustedPlatformAssemblyNames();

    internal static string CommonLanguageRuntimeVersionFolder { get; } = GetCommonLanguageRuntimeVersionFolder();

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
        try
        {
            var tpaList = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string)?.Split(Path.PathSeparator) ?? [];
            return [.. tpaList.Select(Path.GetFileNameWithoutExtension).OfType<string>()];
        }
        catch
        {
            return [];
        }
    }

    private static string GetCommonLanguageRuntimeVersionFolder()
    {
        return $"net{Environment.Version.Major}.{Environment.Version.Minor}";
    }

    private Assembly? Resolving_ManagedProfilerDependencies(AssemblyLoadContext context, AssemblyName assemblyName)
    {
        // TODO do we want to cache this information so we don't need to check and read files every time?
        bool TryFindAssemblyPath(AssemblyName assemblyName, [NotNullWhen(true)] out string? assemblyPath)
        {
            // For .NET (Core) most of the assembblies are different per runtime version so we start first with runtime specific folder
            var runtimeSpecificPath = Path.Combine(_managedProfilerDirectory, CommonLanguageRuntimeVersionFolder, $"{assemblyName.Name}.dll");
            if (File.Exists(runtimeSpecificPath))
            {
                assemblyPath = runtimeSpecificPath;
                return true;
            }

            // if assembly is missing it might be linked, so we check for .link file
            var link = Path.Combine(_managedProfilerDirectory, CommonLanguageRuntimeVersionFolder, $"{assemblyName.Name}.dll.link");
            if (File.Exists(link))
            {
                try
                {
                    var linkRuntimeVersionFolder = File.ReadAllText(link).Trim();
                    var linkPath = Path.Combine(_managedProfilerDirectory, linkRuntimeVersionFolder, $"{assemblyName.Name}.dll");
                    if (File.Exists(linkPath))
                    {
                        assemblyPath = linkPath;
                        return true;
                    }
                    else
                    {
                        _logger.Error($"Linked assembly path \"{linkPath}\" does not exist");
                        assemblyPath = null;
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug(ex, $"Error reading .link file: \"{link}\"");
                    assemblyPath = null;
                    return false;
                }
            }

            // then we fallback to root managed profiler folder
            var rootPath = Path.Combine(_managedProfilerDirectory, $"{assemblyName.Name}.dll");
            if (File.Exists(rootPath))
            {
                assemblyPath = rootPath;
                return true;
            }

            assemblyPath = null;
            return false;
        }

        Assembly? Load()
        {
            // TODO if we still want the mscorlib.resources safeguard to be universal (issue is described in .NET Framework implementation),
            // TODO  we can implement it in runtime-agnostic AssemblyResolver partial class
            // TODO  and make additional no-op check for System.Net.Http in .Net Framework implementation
            // TODO  but skip it for .Net (Core) where we don't redirect this assembly, so this event won't be fired unless there's an external issue we can't fix

            _logger.Debug($"Check assembly {assemblyName}");

            if (!TryFindAssemblyPath(assemblyName, out var assemblyPath))
            {
                _logger.Debug($"Skip loading unexpected assembly {assemblyName}");
                return null;
            }

            // Load conflicting library into a custom ALC
            if (TrustedPlatformAssemblyNames.Contains(assemblyName.Name))
            {
                _logger.Debug("Loading {0} with DependencyLoadContext.LoadFromAssemblyPath", assemblyPath);
                return DependencyLoadContext.LoadFromAssemblyPath(assemblyPath);
            }

            // else load into default ALC
            _logger.Debug("Loading {0} with AssemblyLoadContext.Default.LoadFromAssemblyPath", assemblyPath);
            return AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
        }

        // TODO temporary colored console output for debugging purpose
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.Write($"Resolving <{assemblyName}>@({context}):");

        var assembly = Load();
        if (assembly != null)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($" <{assembly}>@({System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(assembly)}):{assembly.Location}");
        }
        else
        {
            Console.WriteLine(" SKIP");
        }

        Console.ResetColor();
        return assembly;
    }

    private string ResolveManagedProfilerDirectory()
    {
        string tracerFrameworkDirectory = "net";
        string tracerHomeDirectory = ReadEnvironmentVariable("OTEL_DOTNET_AUTO_HOME") ?? string.Empty;

        return Path.Combine(tracerHomeDirectory, tracerFrameworkDirectory);
    }
}
#endif
#pragma warning restore CA1303 // Do not pass literals as localized parameters
