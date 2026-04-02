// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET || NETCOREAPP
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Util;

internal static partial class ManagedProfilerLocationHelper
{
    public static string ManagedProfilerRuntimeDirectory { get; } = Path.Combine(TracerHomeDirectory!, "net");

    public static string ResolveManagedProfilerVersionDirectory(IOtelLogger? logger = null)
    {
        var commonLanguageRuntimeVersionDirectory = $"net{Environment.Version.Major}.{Environment.Version.Minor}";

        logger?.Debug($"Managed Profiler Runtime Directory: {ManagedProfilerRuntimeDirectory}");
        logger?.Debug($"Managed Profiler .NET Version Directory: {commonLanguageRuntimeVersionDirectory}");

        return Path.Combine(ManagedProfilerRuntimeDirectory, commonLanguageRuntimeVersionDirectory);
    }

    public static string? GetAssemblyPath(string assemblyName, IOtelLogger? logger = null)
    {
        var runtimeDir = ManagedProfilerRuntimeDirectory;
        LazyInitializer.EnsureInitialized(ref _managedProfilerVersionDirectory, () => ResolveManagedProfilerVersionDirectory(logger));

        // For .NET (Core) most of the assemblies are different per runtime version, so we
        // 1. first start with runtime version folder           -> e.g. tracer-home/net/net8.0/assembly-name.dll
        // 2. then check .link file in runtime version folder   -> e.g. tracer-home/net/net8.0/assembly-name.dll.link
        // 3. then check runtime root folder                    ->      tracer-home/net/assembly-name.dll
        return Probe(_managedProfilerVersionDirectory, assemblyName) ??
               CheckLinkFile(_managedProfilerVersionDirectory, runtimeDir, assemblyName, logger) ??
               Probe(runtimeDir, assemblyName);
    }
}
#endif
