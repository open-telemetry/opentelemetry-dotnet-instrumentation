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

    internal static string? ResolveAssemblyLink(string linkPath, string runtimeDir, IOtelLogger? logger = null)
    {
        try
        {
            var targetDirName = File.ReadAllText(linkPath).Trim();
#if NETFRAMEWORK
            if (!targetDirName.StartsWith("net", StringComparison.Ordinal)
                || targetDirName.Contains(Path.DirectorySeparatorChar)
                || targetDirName.Contains(Path.AltDirectorySeparatorChar))
#else
            if (!targetDirName.StartsWith("net", StringComparison.Ordinal)
                || targetDirName.Contains(Path.DirectorySeparatorChar, StringComparison.Ordinal)
                || targetDirName.Contains(Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
#endif
            {
                logger?.Error($"Invalid content in .link file \"{linkPath}\". Expected a single directory name starting with 'net', but got: \"{targetDirName}\"");
                return null;
            }

            var targetDirPath = Path.Combine(runtimeDir, targetDirName);
            var assemblyFileName = Path.GetFileNameWithoutExtension(linkPath);
            var assemblyName = Path.GetFileNameWithoutExtension(assemblyFileName);

            var path = Probe(targetDirPath, assemblyName);
            if (path == null)
            {
                logger?.Error($"Linked assembly path \"{Path.Combine(targetDirPath, $"{assemblyFileName}")}\" does not exist");
            }

            return path;
        }
        catch (Exception e)
        {
            logger?.Debug(e, "Error reading .link file {0}", linkPath);
            return null;
        }
    }

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
            return ResolveAssemblyLink(linkFile, runtimeDir, logger);
        }

        return null;
    }
}
