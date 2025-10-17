// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Win32;

namespace OpenTelemetry.AutoInstrumentation.Loader;

/// <summary>
/// A class that attempts to load the OpenTelemetry.AutoInstrumentation .NET assembly.
/// </summary>
/// [ToDo]: Change file name in the next PR. Remove suppress.
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:ElementsMustBeOrderedByAccess", Justification = "Make code review easy.")]
internal partial class AssemblyResolver
{
    private static string ResolveManagedProfilerDirectory()
    {
        var tracerHomeDirectory = ReadEnvironmentVariable("OTEL_DOTNET_AUTO_HOME") ?? string.Empty;
        var tracerFrameworkDirectory = "netfx";
        var frameworkVersion = GetNetFrameworkVersionFolder();

        return Path.Combine(tracerHomeDirectory, tracerFrameworkDirectory, frameworkVersion);
    }

    private static string GetNetFrameworkVersionFolder()
    {
        try
        {
            // Try to get version from Windows Registry first (most reliable method)
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
            using var subKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\");
            var releaseValue = subKey?.GetValue("Release");
            if (releaseValue is int release)
            {
                // Map release number to framework version number
                // Based on https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed
                if (release >= 461808)
                {
                    return "net472"; // .NET Framework 4.7.2
                }

                if (release >= 461308)
                {
                    return "net471"; // .NET Framework 4.7.1
                }

                if (release >= 460798)
                {
                    return "net47"; // .NET Framework 4.7
                }
            }
        }
        catch (Exception ex)
        {
            logger.Debug(ex, "Error getting .NET Framework version from Windows Registry");
        }

        return "net462";
    }

    internal static Assembly? AssemblyResolve_ManagedProfilerDependencies(object sender, ResolveEventArgs args)
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
                logger.Debug<string, bool>("Assembly.Load(\"{0}\") succeeded={1}", assemblyName, mongoAssembly != null);
                return mongoAssembly;
            }
            catch (Exception ex)
            {
                logger.Debug(ex, "Assembly.Load(\"{0}\") Exception: {1}", assemblyName, ex.Message);
            }

            return null;
        }

        var path = Path.Combine(Path.GetDirectoryName(ManagedProfilerDirectory) ?? ManagedProfilerDirectory, $"{assemblyName}.dll");
        if (!File.Exists(path))
        {
            path = Path.Combine(ManagedProfilerDirectory, $"{assemblyName}.dll");
            if (!File.Exists(path))
            {
                var link = Path.Combine(ManagedProfilerDirectory, $"{assemblyName}.dll.link");
                if (File.Exists(link))
                {
                    try
                    {
                        var linkPath = File.ReadAllText(link).Trim();
                        path = Path.Combine(Path.GetDirectoryName(ManagedProfilerDirectory) ?? ManagedProfilerDirectory, linkPath, $"{assemblyName}.dll");
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
#endif
