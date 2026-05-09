// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation.Loader;

internal class AssemblyCatalog
{
    private readonly Dictionary<string, AssemblyInfo> _assemblies = new(StringComparer.OrdinalIgnoreCase);

    public AssemblyCatalog(IOtelLogger logger)
    {
        void BuildFromFolder()
        {
            var frameworkPath = ManagedProfilerLocationHelper.ResolveManagedProfilerVersionDirectory(logger);
            var sharedFrameworkPath = ManagedProfilerLocationHelper.ManagedProfilerRuntimeDirectory;

            var files = Directory.GetFiles(frameworkPath, "*.dll")
                    .Concat(Directory
                        .GetFiles(frameworkPath, "*.link")
                        .Select(
                            link => ManagedProfilerLocationHelper.ResolveAssemblyLink(link, sharedFrameworkPath, logger))
                        .OfType<string>())
                    .Concat(Directory.GetFiles(sharedFrameworkPath, "*.dll"));

            foreach (var file in files)
            {
                try
                {
                    var assemblyName = AssemblyName.GetAssemblyName(file);
                    var keyToken = assemblyName.GetPublicKeyToken();
                    if (assemblyName.Name == null || keyToken == null || keyToken.Length == 0 ||
                        assemblyName.Version == null)
                    {
                        logger.Warning($"No strong name for {file} ({assemblyName}), skipping it");
                        continue;
                    }

#pragma warning disable CA1308
                    var token = BitConverter.ToString(keyToken).ToLowerInvariant().Replace("-", string.Empty);
#pragma warning restore CA1308

                    if (_assemblies.TryGetValue(assemblyName.Name, out var info))
                    {
                        if (!string.Equals(info.Token, token, StringComparison.OrdinalIgnoreCase))
                        {
                            logger.Error(
                                $"Multiple files for {assemblyName.Name} with different tokens. Using {info.Path}");
                            continue;
                        }

                        if (info.Version < assemblyName.Version)
                        {
                            logger.Warning(
                                $"Multiple files for {assemblyName.Name}, using {assemblyName.Version} from {file}");
                        }
                        else
                        {
                            logger.Warning(
                                $"Multiple files for {assemblyName.Name}, using {info.Version} from {info.Path}");
                            continue;
                        }
                    }

                    _assemblies[assemblyName.Name] = new AssemblyInfo(token, assemblyName.Version, ReadFileVersion(file), assemblyName, file);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Failed to resolve assembly name for {file}, skipping it");
                }
            }
        }

        BuildFromFolder();
    }

    internal AssemblyCatalog(IEnumerable<AssemblyInfo> assemblies)
    {
        foreach (var assembly in assemblies)
        {
            _assemblies[assembly.FullName.Name!] = assembly;
        }
    }

    internal AssemblyInfo? GetAssemblyInfo(string shortName)
    {
        if (_assemblies.TryGetValue(shortName, out var info))
        {
            return info;
        }

        return null;
    }

    internal IEnumerable<AssemblyInfo> GetAssemblies()
        => _assemblies.Values;

    private static Version? ReadFileVersion(string path)
    {
        try
        {
            var fileVersion = FileVersionInfo.GetVersionInfo(path).FileVersion;
            return string.IsNullOrWhiteSpace(fileVersion) ? null : new Version(fileVersion);
        }
        catch (Exception)
        {
            return null;
        }
    }

    internal sealed class AssemblyInfo(string token, Version version, Version? fileVersion, AssemblyName fullName, string path)
    {
        public string Token { get; } = token;

        public Version Version { get; } = version;

        public Version? FileVersion { get; } = fileVersion;

        public AssemblyName FullName { get; } = fullName;

        public string Path { get; } = path;
   }
}
#endif
