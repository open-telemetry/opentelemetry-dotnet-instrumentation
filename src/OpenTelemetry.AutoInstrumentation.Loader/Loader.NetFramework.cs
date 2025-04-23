// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK

using System.Reflection;

namespace OpenTelemetry.AutoInstrumentation.Loader;

/// <summary>
/// A class that attempts to load the OpenTelemetry.AutoInstrumentation .NET assembly.
/// </summary>
internal partial class Loader
{
    private static bool _isEarlyResolverInstalled;

    static partial void Init()
    {
        // Validate if early assembly resolver was not installed.
        // Mostly it will be in case of tests.
        // But it means, that test behaviour and real execution will be different.
        _isEarlyResolverInstalled =
            typeof(AppDomain).GetMethod("__otel_assembly_resolver__", BindingFlags.NonPublic | BindingFlags.Static) != null;
    }

    private static string ResolveManagedProfilerDirectory()
    {
        var tracerHomeDirectory = ReadEnvironmentVariable("OTEL_DOTNET_AUTO_HOME") ?? string.Empty;
        var tracerFrameworkDirectory = "netfx";
        return Path.Combine(tracerHomeDirectory, tracerFrameworkDirectory);
    }

    private static Assembly? AssemblyResolve_ManagedProfilerDependencies(object sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name;

        // On .NET Framework, having a non-US locale can cause mscorlib
        // to enter the AssemblyResolve event when searching for resources
        // in its satellite assemblies. Exit early so we don't cause
        // infinite recursion.
        if (string.Equals(assemblyName, "mscorlib.resources", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(assemblyName, "System.Net.Http", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        Logger.Debug("Requester [{0}] requested [{1}]", args.RequestingAssembly?.FullName ?? "<null>", args.Name ?? "<null>");

        // All MongoDB* are signed and does not follow https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/versioning#assembly-version
        // There is no possibility to automatically redirect from 2.28.0 to 2.29.0.
        // Loading assembly and ignoring this version.
        if (assemblyName.StartsWith("MongoDB", StringComparison.OrdinalIgnoreCase) &&
            (string.Equals(assemblyName, "MongoDB.Driver.Core", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(assemblyName, "MongoDB.Bson", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(assemblyName, "MongoDB.Libmongocrypt", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var mongoAssembly = Assembly.Load(assemblyName);
                Logger.Debug<string, bool>("Assembly.Load(\"{0}\") succeeded={1}", assemblyName, mongoAssembly != null);
                return mongoAssembly;
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Assembly.Load(\"{0}\") Exception: {1}", assemblyName, ex.Message);
            }

            return null;
        }

        var path = Path.Combine(ManagedProfilerDirectory, $"{assemblyName}.dll");
        if (File.Exists(path))
        {
            if (!AppDomain.CurrentDomain.IsDefaultAppDomain() && _isEarlyResolverInstalled)
            {
                // If assembly with same name already loaded, use it instead of trying to load another version
                // That probably should be done even in primary app domain
                var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(assembly => assembly.GetName().Name == assemblyName);
                if (loadedAssembly != null)
                {
                    Logger.Debug($"Resolve {args.Name} to {loadedAssembly.FullName} already loaded assembly (non-default domain)");
                    return loadedAssembly;
                }

                // OpenTelemetry.AutoInstrumentation assembly should be loaded by resolver
                // registered in AppDomain.Setup. If it is not found yet, it is still too early
                // to resolve any other assemblies - we may not fixed versions for them yet, as they are not yet loaded
                // only check if they can be resolved
                // If OpenTelemetry.AutoInstrumentation loaded from GAC, we can't load any other assemblies here,
                // as it is too late for it.
                var otelAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(assembly => assembly.GetName().Name == "OpenTelemetry.AutoInstrumentation");
                if (otelAssembly == null || otelAssembly.GlobalAssemblyCache)
                {
                    Logger.Debug($"Do not resolve {assemblyName} when OpenTelemetry.AutoInstrumentation assembly loaded from GAC in non-default domain");
                    return null;
                }
            }

            try
            {
                var loadedAssembly = Assembly.LoadFrom(path);
                Logger.Debug<string, bool>("Assembly.LoadFrom(\"{0}\") succeeded={1}", path, loadedAssembly != null);
                return loadedAssembly;
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Assembly.LoadFrom(\"{0}\") Exception: {1}", path, ex.Message);
            }
        }

        return null;
    }
}
#endif
