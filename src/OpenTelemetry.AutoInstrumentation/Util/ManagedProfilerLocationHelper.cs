// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Util;

internal static partial class ManagedProfilerLocationHelper
{
    private static string? _managedProfilerVersionDirectory;

    public static string TracerHomeDirectory { get; } =
        ReadEnvironmentVariable(Constants.EnvironmentVariables.OtelDotnetAutoHome) ?? string.Empty;

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

    private static string? Probe(string dir, string name)
    {
        var path = Path.Combine(dir, $"{name}.dll");
        return File.Exists(path) ? path : null;
    }

    private static string? CheckLinkFile(string versionDir, string runtimeDir, string name, IOtelLogger? logger = null)
    {
        var linkFile = Path.Combine(versionDir, $"{name}.dll.link");
        if (File.Exists(linkFile))
        {
            try
            {
                var targetDirName = File.ReadAllText(linkFile).Trim();
                var targetDirPath = Path.Combine(runtimeDir, targetDirName);
                var path = Probe(targetDirPath, name);
                if (path == null)
                {
                    logger?.Error($"Linked assembly path \"{Path.Combine(targetDirPath, $"{name}.dll")}\" does not exist");
                    return null;
                }

                return path;
            }
            catch (Exception ex)
            {
                logger?.Debug(ex, "Error reading .link file {0}", linkFile);
            }
        }

        return null;
    }
}
