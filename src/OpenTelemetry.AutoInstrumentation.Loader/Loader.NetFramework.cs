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

        Logger.Debug("Requester [{0}] requested [{1}]", args?.RequestingAssembly?.FullName ?? "<null>", args?.Name ?? "<null>");
        var path = Path.Combine(ManagedProfilerDirectory, $"{assemblyName}.dll");
        if (File.Exists(path))
        {
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
