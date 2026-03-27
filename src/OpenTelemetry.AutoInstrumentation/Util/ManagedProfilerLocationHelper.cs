// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Util;

internal static partial class ManagedProfilerLocationHelper
{
    private static string? _managedProfilerVersionDirectory;

    public static string TracerHomeDirectory { get; } =
        ReadEnvironmentVariable(ConfigurationKeys.TracerHome) ?? string.Empty;

    private static string? ReadEnvironmentVariable(string key)
    {
        try
        {
            return Environment.GetEnvironmentVariable(key);
        }
        catch
        {
            return null;
        }
    }

    private static AssemblyLocation? Probe(string dir, string name, bool isStandalone)
    {
        var path = Path.Combine(dir, $"{name}.dll");
        return File.Exists(path) ? new AssemblyLocation { Path = path, IsStandalone = isStandalone } : null;
    }

    private static AssemblyLocation? CheckLinkFile(string versionDir, string runtimeDir, string name, bool isStandalone, IOtelLogger? logger = null)
    {
        var linkFile = Path.Combine(versionDir, $"{name}.dll.link");
        if (File.Exists(linkFile))
        {
            try
            {
                var targetDirName = File.ReadAllText(linkFile).Trim();
                var targetDirPath = Path.Combine(runtimeDir, targetDirName);
                var location = Probe(targetDirPath, name, isStandalone);
                if (location == null)
                {
                    logger?.Error($"Linked assembly path \"{Path.Combine(targetDirPath, $"{name}.dll")}\" does not exist");
                }

                return location;
            }
            catch (Exception ex)
            {
                logger?.Debug(ex, "Error reading .link file {0}", linkFile);
            }
        }

        return null;
    }

    /// <summary>
    /// Represents the result of a managed assembly search.
    /// </summary>
    internal record struct AssemblyLocation(string Path, bool IsStandalone)
    {
    }
}
