// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
#if NETFRAMEWORK
using System.Runtime.InteropServices;
#endif
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Loader;

internal static class EnvironmentHelper
{
    public const string LoaderLoggerSuffix = "Loader";
    private static int _isLogCloseCalled;

    static EnvironmentHelper()
    {
        Logger = OtelLogging.GetLogger(LoaderLoggerSuffix);
        ManagedProfilerDirectory = ResolveManagedProfilerDirectory();

        AppDomain.CurrentDomain.ProcessExit += (_, _) => CloseLogger();
    }

    internal static string ManagedProfilerDirectory { get; }

    internal static IOtelLogger Logger { get; }

    internal static void CloseLogger()
    {
        if (Interlocked.Exchange(ref _isLogCloseCalled, value: 1) != 0)
        {
            // CloseLogger() was already called before
            return;
        }

        OtelLogging.CloseLogger(LoaderLoggerSuffix, Logger);
    }

#if NETFRAMEWORK
    /// <summary>
    /// Return redirection table used in runtime that will match TFM folder to load assemblies.
    /// It may not be actual .NET Framework version.
    /// </summary>
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
    private static extern int GetNetFrameworkRedirectionVersion();

    private static string ResolveManagedProfilerDirectory()
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
                Logger.Warning($"Framework folder {candidateFolderName} not found. Fallback to {frameworkFolderName}.");
            }
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, $"Error getting .NET Framework version from native profiler. Fallback to {frameworkFolderName}.");
        }

        return Path.Combine(basePath, frameworkFolderName);
    }
#else
    private static string ResolveManagedProfilerDirectory()
    {
        var tracerHomeDirectory = ReadEnvironmentVariable("OTEL_DOTNET_AUTO_HOME") ?? string.Empty;
        var tracerFrameworkDirectory = "net";
        var basePath = Path.Combine(tracerHomeDirectory, tracerFrameworkDirectory);
        return basePath;
    }
#endif

    private static string? ReadEnvironmentVariable(string key)
    {
        try
        {
            return Environment.GetEnvironmentVariable(key);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error while loading environment variable {0}", key);
        }

        return null;
    }
}
