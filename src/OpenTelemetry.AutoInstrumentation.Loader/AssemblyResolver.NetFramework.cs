// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using System.Runtime.InteropServices;

namespace OpenTelemetry.AutoInstrumentation.Loader;

/// <summary>
/// A class that attempts to load the OpenTelemetry.AutoInstrumentation .NET assembly.
/// </summary>
internal partial class AssemblyResolver
{
    internal void RegisterAssemblyResolving()
    {
        AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve_ManagedProfilerDependencies;
    }

    /// <summary>
    /// Return redirection table used in runtime that will match TFM folder to load assemblies.
    /// It may not be actual .NET Framework version.
    /// </summary>
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
    private static extern int GetNetFrameworkRedirectionVersion();

    private Assembly? AssemblyResolve_ManagedProfilerDependencies(object sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name;
        logger.Debug($"Check assembly {assemblyName}");

        // On .NET Framework, having a non-US locale can cause mscorlib
        // to enter the AssemblyResolve event when searching for resources
        // in its satellite assemblies. Exit early so we don't cause
        // infinite recursion.
        if (string.Equals(assemblyName, "mscorlib.resources", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(assemblyName, "System.Net.Http", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        logger.Debug("Requester [{0}] requested [{1}]", args.RequestingAssembly?.FullName ?? "<null>", args.Name ?? "<null>");

        var path = Path.Combine(_managedProfilerRuntimeDirectory, $"{assemblyName}.dll");
        if (!File.Exists(path))
        {
            path = Path.Combine(_managedProfilerVersionDirectory, $"{assemblyName}.dll");
            if (!File.Exists(path))
            {
                var link = Path.Combine(_managedProfilerVersionDirectory, $"{assemblyName}.dll.link");
                if (File.Exists(link))
                {
                    try
                    {
                        var linkPath = File.ReadAllText(link).Trim();
                        path = Path.Combine(_managedProfilerRuntimeDirectory, linkPath, $"{assemblyName}.dll");
                    }
                    catch (Exception ex)
                    {
                        logger.Debug(ex, "Error reading .link file {0}", link);
                    }
                }
                else
                {
                    // Not found
                    return null;
                }
            }
        }

        if (File.Exists(path))
        {
            try
            {
                var loadedAssembly = Assembly.LoadFrom(path);
                logger.Debug<string, bool>("Assembly.LoadFrom(\"{0}\") succeeded={1}", path, loadedAssembly != null);
                return loadedAssembly;
            }
            catch (Exception ex)
            {
                logger.Debug(ex, "Assembly.LoadFrom(\"{0}\") Exception: {1}", path, ex.Message);
            }
        }

        return null;
    }
}
