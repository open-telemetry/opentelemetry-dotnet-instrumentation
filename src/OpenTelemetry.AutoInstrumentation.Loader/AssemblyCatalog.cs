// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using System.Reflection;

namespace OpenTelemetry.AutoInstrumentation.Loader;

internal class AssemblyCatalog
{
    private static readonly Dictionary<string, AssemblyInfo> Assemblies = new(StringComparer.OrdinalIgnoreCase);

    static AssemblyCatalog()
    {
        static void BuildFromFolder()
        {
            var sharedFrameworkPath = Path.GetDirectoryName(EnvironmentHelper.ManagedProfilerDirectory)!;

            var files =
                Directory.GetFiles(sharedFrameworkPath, "*.dll")
                    .Concat(
                        Directory.GetFiles(EnvironmentHelper.ManagedProfilerDirectory, "*.dll"))
                    .Concat(Directory.GetFiles(EnvironmentHelper.ManagedProfilerDirectory, "*.link").Select(link =>
                    {
                        var linkPath = File.ReadAllText(link).Trim();
                        return Path.Combine(sharedFrameworkPath, linkPath, Path.GetFileNameWithoutExtension(link));
                    }));

            foreach (var file in files)
            {
                try
                {
                    var assemblyName = AssemblyName.GetAssemblyName(file);
                    var keyToken = assemblyName.GetPublicKeyToken();
                    if (assemblyName.Name == null || keyToken == null || keyToken.Length == 0 ||
                        assemblyName.Version == null)
                    {
                        EnvironmentHelper.Logger.Warning($"No strong name for {file} ({assemblyName}), skipping it");
                        continue;
                    }

#pragma warning disable CA1308
                    var token = BitConverter.ToString(keyToken).ToLowerInvariant().Replace("-", string.Empty);
#pragma warning restore CA1308

                    if (Assemblies.TryGetValue(assemblyName.Name, out var info))
                    {
                        if (!string.Equals(info.Token, token, StringComparison.OrdinalIgnoreCase))
                        {
                            EnvironmentHelper.Logger.Error(
                                $"Multiple files for {assemblyName.Name} with different tokens. Using {file}");
                            continue;
                        }

                        if (info.Version < assemblyName.Version)
                        {
                            EnvironmentHelper.Logger.Warning(
                                $"Multiple files for {assemblyName.Name}, using ${assemblyName.Version} from {file}");
                        }
                        else
                        {
                            EnvironmentHelper.Logger.Warning(
                                $"Multiple files  for {assemblyName.Name}, using ${info.Version} from {info.Path}");
                            continue;
                        }
                    }

                    Assemblies[assemblyName.Name] = new AssemblyInfo(token, assemblyName.Version, assemblyName, file);
                }
                catch (Exception ex)
                {
                    EnvironmentHelper.Logger.Error(ex, $"Failed to resolve assembly name for {file}, skipping it");
                }
            }
        }

        BuildFromFolder();
    }

    internal static AssemblyInfo? GetAssemblyInfo(string shortName)
    {
        if (Assemblies.TryGetValue(shortName, out var info))
        {
            return info;
        }

        return null;
    }

    internal static IEnumerable<AssemblyInfo> GetAssemblies()
        => Assemblies.Values;

    internal sealed class AssemblyInfo(string token, Version version, AssemblyName fullName, string path)
    {
        public string Token { get; } = token;

        public Version Version { get; } = version;

        public AssemblyName FullName { get; } = fullName;

        public string Path { get; } = path;
   }
}
#endif
