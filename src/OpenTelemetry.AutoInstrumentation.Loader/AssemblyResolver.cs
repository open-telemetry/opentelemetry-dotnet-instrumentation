// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Loader;

/// <summary>
/// A class that help Loader to load the OpenTelemetry.AutoInstrumentation .NET assembly.
/// </summary>
internal partial class AssemblyResolver
{
    private const string AssemblyResolverLoggerSuffix = "AssemblyResolver";
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger(AssemblyResolverLoggerSuffix);

    private static readonly string ManagedProfilerDirectory;

    /// <summary>
    /// Initializes static members of the <see cref="AssemblyResolver"/> class.
    /// </summary>
    static AssemblyResolver()
    {
        ManagedProfilerDirectory = ResolveManagedProfilerDirectory();
    }

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
