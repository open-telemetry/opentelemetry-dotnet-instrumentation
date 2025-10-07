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
    private static readonly string ManagedProfilerDirectory;

    private static IOtelLogger logger = NoopLogger.Instance;

    /// <summary>
    /// Initializes static members of the <see cref="AssemblyResolver"/> class.
    /// </summary>
    static AssemblyResolver()
    {
        ManagedProfilerDirectory = ResolveManagedProfilerDirectory();
    }

    // This method is not thread safe. To avoid adding lock here, it should only be
    // called in the static constructor of Loader.
    internal static void SetLoggerNoLock(IOtelLogger otelLogger)
    {
        logger = otelLogger;
    }

    private static string? ReadEnvironmentVariable(string key)
    {
        try
        {
            return Environment.GetEnvironmentVariable(key);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error while loading environment variable {0}", key);
        }

        return null;
    }
}
