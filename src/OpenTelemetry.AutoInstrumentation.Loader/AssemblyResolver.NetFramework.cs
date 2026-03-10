// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK

using System.Reflection;
using System.Runtime.InteropServices;

namespace OpenTelemetry.AutoInstrumentation.Loader;

/// <summary>
/// A class that attempts to load the OpenTelemetry.AutoInstrumentation .NET assembly.
/// </summary>
internal partial class AssemblyResolver
{
    internal Assembly? AssemblyResolve_ManagedProfilerDependencies(object sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name;
        _logger.Debug($"Check assembly {assemblyName}");

        // On .NET Framework, having a non-US locale can cause mscorlib
        // to enter the AssemblyResolve event when searching for resources
        // in its satellite assemblies. Exit early so we don't cause
        // infinite recursion.
        if (string.Equals(assemblyName, "mscorlib.resources", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(assemblyName, "System.Net.Http", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        _logger.Debug("Requester [{0}] requested [{1}]", args.RequestingAssembly?.FullName ?? "<null>", args.Name ?? "<null>");

        var path = Path.Combine(Path.GetDirectoryName(_managedProfilerDirectory) ?? _managedProfilerDirectory, $"{assemblyName}.dll");
        if (!File.Exists(path))
        {
            path = Path.Combine(_managedProfilerDirectory, $"{assemblyName}.dll");
            if (!File.Exists(path))
            {
                var link = Path.Combine(_managedProfilerDirectory, $"{assemblyName}.dll.link");
                if (File.Exists(link))
                {
                    try
                    {
                        var linkPath = File.ReadAllText(link).Trim();
                        path = Path.Combine(Path.GetDirectoryName(_managedProfilerDirectory) ?? _managedProfilerDirectory, linkPath, $"{assemblyName}.dll");
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug(ex, "Error reading .link file {0}", link);
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
                _logger.Debug<string, bool>("Assembly.LoadFrom(\"{0}\") succeeded={1}", path, loadedAssembly != null);
                return loadedAssembly;
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Assembly.LoadFrom(\"{0}\") Exception: {1}", path, ex.Message);
            }
        }

        return null;
    }

    /// <summary>
    /// Return redirection table used in runtime that will match TFM folder to load assemblies.
    /// It may not be actual .NET Framework version.
    /// </summary>
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
    private static extern int GetNetFrameworkRedirectionVersion();

    private string ResolveManagedProfilerDirectory()
    {
        var tracerHomeDirectory = ReadEnvironmentVariable("OTEL_DOTNET_AUTO_HOME") ?? string.Empty;
        var tracerFrameworkDirectory = "netfx";

        var basePath = Path.Combine(tracerHomeDirectory, tracerFrameworkDirectory);
        // fallback to net462 in case of any issues
        var frameworkFolderName = "net462";
        try
        {
            var detectedVersion = GetNetFrameworkRedirectionVersion();
            var candidateFolderName = detectedVersion % 10 != 0 ? $"net{detectedVersion}" : $"net{detectedVersion / 10}";
            if (Directory.Exists(Path.Combine(basePath, candidateFolderName)))
            {
                frameworkFolderName = candidateFolderName;
            }
            else
            {
                _logger.Warning($"Framework folder {candidateFolderName} not found. Fallback to {frameworkFolderName}.");
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, $"Error getting .NET Framework version from native profiler. Fallback to {frameworkFolderName}.");
        }

        return Path.Combine(basePath, frameworkFolderName);
    }
}
#endif
