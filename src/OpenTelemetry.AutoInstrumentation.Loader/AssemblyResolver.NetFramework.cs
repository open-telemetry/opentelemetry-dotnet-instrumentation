// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation.Loader;

/// <summary>
/// A class that attempts to load the OpenTelemetry.AutoInstrumentation .NET assembly.
/// </summary>
internal class AssemblyResolver(IOtelLogger logger)
{
    internal void RegisterAssemblyResolving()
    {
        AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve_ManagedProfilerDependencies;
    }

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

        var assemblyPath = ManagedProfilerLocationHelper.GetAssemblyPath(assemblyName, logger);
        if (assemblyPath is null)
        {
            logger.Debug($"Skip resolving unexpected assembly: ({assemblyName})");
            return null;
        }

        try
        {
            var loadedAssembly = Assembly.LoadFrom(assemblyPath);
            logger.Debug<string, bool>("Assembly.LoadFrom(\"{0}\") succeeded={1}", assemblyPath, loadedAssembly != null);
            return loadedAssembly;
        }
        catch (Exception ex)
        {
            logger.Debug(ex, "Assembly.LoadFrom(\"{0}\") Exception: {1}", assemblyPath, ex.Message);
        }

        return null;
    }
}
