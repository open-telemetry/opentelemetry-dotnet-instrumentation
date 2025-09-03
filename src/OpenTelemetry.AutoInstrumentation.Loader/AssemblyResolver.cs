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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1306:Field names should begin with lower-case letter", Justification = "Make code review easy. Will fix in next PR")]
    private static IOtelLogger Logger = NoopLogger.Instance;

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
        Logger = otelLogger;
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
