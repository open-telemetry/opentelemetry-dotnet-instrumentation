// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Util;

internal static partial class ManagedProfilerLocationHelper
{
    public static string ResolveManagedProfilerDirectory(IOtelLogger logger)
    {
        var frameworkDirectoryName = "netfx";
        var fallbackFrameworkFolderName = "net462";

        try
        {
            var detectedVersion = GetNetFrameworkRedirectionVersion();
            var detectedDirectoryName = detectedVersion % 10 != 0 ? $"net{detectedVersion}" : $"net{detectedVersion / 10}";
            var detectedDirectoryPath = Path.Combine(TracerHomeDirectory, frameworkDirectoryName, detectedDirectoryName);

            if (Directory.Exists(detectedDirectoryPath))
            {
                return detectedDirectoryPath;
            }

            logger.Warning($"Framework folder {detectedDirectoryName} not found. Fallback to {fallbackFrameworkFolderName}.");
        }
        catch (Exception ex)
        {
            logger.Warning(ex, $"Error getting .NET Framework version from native profiler. Fallback to {fallbackFrameworkFolderName}.");
        }

        return Path.Combine(TracerHomeDirectory, frameworkDirectoryName, fallbackFrameworkFolderName);
    }

    [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int GetNetFrameworkRedirectionVersion();
}
