// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;

namespace OpenTelemetry.AutoInstrumentation.Loader;

/// <summary>
/// A class that attempts to load the OpenTelemetry.AutoInstrumentation .NET assembly.
/// </summary>
/// [ToDo]: Change file name in the next PR. Remove suppress.
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:FileNameMustMatchTypeName", Justification = "Make code review easy.")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:ElementsMustBeOrderedByAccess", Justification = "Make code review easy.")]
internal partial class AssemblyResolver
{
    internal static System.Runtime.Loader.AssemblyLoadContext DependencyLoadContext { get; } = new ManagedProfilerAssemblyLoadContext();

    internal static string[]? StoreFiles { get; } = GetStoreFiles();

    private static string ResolveManagedProfilerDirectory()
    {
        string tracerFrameworkDirectory = "net";
        string tracerHomeDirectory = ReadEnvironmentVariable("OTEL_DOTNET_AUTO_HOME") ?? string.Empty;

        return Path.Combine(tracerHomeDirectory, tracerFrameworkDirectory);
    }

    private static string[]? GetStoreFiles()
    {
        try
        {
            var storeDirectory = Environment.GetEnvironmentVariable("DOTNET_SHARED_STORE");
            if (storeDirectory == null || !Directory.Exists(storeDirectory))
            {
                return null;
            }

            var architecture = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X86 => "x86",
                Architecture.Arm64 => "arm64",
                _ => "x64" // Default to x64 for architectures not explicitly handled
            };

            var targetFramework = $"net{Environment.Version.Major}.{Environment.Version.Minor}";
            var finalPath = Path.Combine(storeDirectory, architecture, targetFramework);

            var storeFiles = Directory.GetFiles(finalPath, "Microsoft.Extensions*.dll", SearchOption.AllDirectories);
            return storeFiles;
        }
        catch
        {
            return null;
        }
    }

    internal static Assembly? AssemblyResolve_ManagedProfilerDependencies(object? sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name);

        // On .NET Framework, having a non-US locale can cause mscorlib
        // to enter the AssemblyResolve event when searching for resources
        // in its satellite assemblies. This seems to have been fixed in
        // .NET Core in the 2.0 servicing branch, so we should not see this
        // occur, but guard against it anyways. If we do see it, exit early
        // so we don't cause infinite recursion.
        if (string.Equals(assemblyName.Name, "System.Private.CoreLib.resources", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(assemblyName.Name, "System.Net.Http", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var path = Path.Combine(ManagedProfilerDirectory, $"{assemblyName.Name}.dll");

        // Only load the main profiler into the default Assembly Load Context.
        // If OpenTelemetry.AutoInstrumentation or other libraries are provided by the NuGet package their loads are handled in the following two ways.
        // 1) The AssemblyVersion is greater than or equal to the version used by OpenTelemetry.AutoInstrumentation, the assembly
        //    will load successfully and will not invoke this resolve event.
        // 2) The AssemblyVersion is lower than the version used by OpenTelemetry.AutoInstrumentation, the assembly will fail to load
        //    and invoke this resolve event. It must be loaded in a separate AssemblyLoadContext since the application will only
        //    load the originally referenced version
        if (assemblyName.Name != null && assemblyName.Name.StartsWith("OpenTelemetry.AutoInstrumentation", StringComparison.OrdinalIgnoreCase) && File.Exists(path))
        {
            Logger.Debug("Loading {0} with Assembly.LoadFrom", path);
            return Assembly.LoadFrom(path);
        }
        else if (File.Exists(path))
        {
            Logger.Debug("Loading {0} with DependencyLoadContext.LoadFromAssemblyPath", path);
            return DependencyLoadContext.LoadFromAssemblyPath(path); // Load unresolved framework and third-party dependencies into a custom Assembly Load Context
        }
        else
        {
            var entry = StoreFiles?.FirstOrDefault(e => e.EndsWith($"{assemblyName.Name}.dll"));
            if (entry != null)
            {
                return DependencyLoadContext.LoadFromAssemblyPath(entry);
            }

            return null;
        }
    }
}
#endif
