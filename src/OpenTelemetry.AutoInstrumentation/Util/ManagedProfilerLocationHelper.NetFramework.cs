// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using System.Runtime.InteropServices;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Util;

internal static partial class ManagedProfilerLocationHelper
{
    public static string ManagedProfilerRuntimeDirectory { get; } = Path.Combine(TracerHomeDirectory, "netfx");

    public static string ResolveManagedProfilerVersionDirectory(IOtelLogger logger)
    {
        var frameworkDirectoryName = "net462";

        try
        {
            var detectedVersion = GetNetFrameworkRedirectionVersion();
            var detectedDirectoryName = detectedVersion % 10 != 0 ? $"net{detectedVersion}" : $"net{detectedVersion / 10}";

            if (Directory.Exists(Path.Combine(ManagedProfilerRuntimeDirectory, detectedDirectoryName)))
            {
                frameworkDirectoryName = detectedDirectoryName;
            }
            else
            {
                logger.Warning($"Framework folder {detectedDirectoryName} not found. Fallback to {frameworkDirectoryName}.");
            }
        }
        catch (Exception ex)
        {
            logger.Warning(ex, $"Error getting .NET Framework version from native profiler. Fallback to {frameworkDirectoryName}.");
        }

        logger.Debug($"Managed Profiler Runtime Directory: {ManagedProfilerRuntimeDirectory}");
        logger.Debug($"Managed Profiler .NET Framework Version Directory: {frameworkDirectoryName}");

        return Path.Combine(ManagedProfilerRuntimeDirectory, frameworkDirectoryName);
    }

    public static string? GetAssemblyPath(string assemblyName, IOtelLogger logger)
    {
        var runtimeDir = ManagedProfilerRuntimeDirectory;
        LazyInitializer.EnsureInitialized(ref _managedProfilerVersionDirectory, () => ResolveManagedProfilerVersionDirectory(logger));

        // For .NET Framework most of the assembblies are common, so we
        // 1. first start with runtime root folder                      ->      tracer-home/netfx/assembly-name.dll
        // 2. then check runtime version folder                         -> e.g. tracer-home/netfx/net462/assembly-name.dll
        // 3. last fallback to .link file in runtime version folder     -> e.g. tracer-home/netfx/net462/assembly-name.dll.link
        return Probe(runtimeDir, assemblyName) ??
               Probe(_managedProfilerVersionDirectory!, assemblyName) ??
               CheckLinkFile(_managedProfilerVersionDirectory!, runtimeDir, assemblyName, logger);
    }

    [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int GetNetFrameworkRedirectionVersion();
}
#endif
