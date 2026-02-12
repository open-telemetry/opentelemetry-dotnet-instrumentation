// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

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

        logger.Debug($"Using .NET Framework folder: {frameworkDirectoryName}");

        return Path.Combine(ManagedProfilerRuntimeDirectory, frameworkDirectoryName);
    }

    public static string? GetAssemblyPath(string assemblyName, IOtelLogger logger)
    {
        var runtimeDir = ManagedProfilerRuntimeDirectory;
        LazyInitializer.EnsureInitialized(ref _managedProfilerVersionDirectory, () => ResolveManagedProfilerVersionDirectory(logger));

        // Framework Order: 1. RuntimeDir -> 2. VersionDir -> 3. Link (in VersionDir)
        // For .NET Framework most of the assembblies are common, so we
        // 3. first start with runtime root folder (tracer-home/netfx)
        // 1. then check runtime version folder (e.g., tracer-home/netfx/net462)
        // 2. last fallback to .link file in runtime version folder
        return Probe(runtimeDir, assemblyName) ??
               Probe(_managedProfilerVersionDirectory!, assemblyName) ??
               CheckLinkFile(_managedProfilerVersionDirectory!, runtimeDir, assemblyName, logger);
    }

    [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int GetNetFrameworkRedirectionVersion();
}
