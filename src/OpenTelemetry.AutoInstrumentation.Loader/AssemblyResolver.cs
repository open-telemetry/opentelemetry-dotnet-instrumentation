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
    private readonly string _managedProfilerDirectory;

    private readonly IOtelLogger _logger;

    public AssemblyResolver(IOtelLogger otelLogger)
    {
        _logger = otelLogger;
        _managedProfilerDirectory = ResolveManagedProfilerDirectory();
    }

    private string? ReadEnvironmentVariable(string key)
    {
        try
        {
            return Environment.GetEnvironmentVariable(key);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error while loading environment variable {0}", key);
        }

        return null;
    }
}
