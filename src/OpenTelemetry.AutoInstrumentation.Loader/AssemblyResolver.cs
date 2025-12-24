// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

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
        _managedProfilerDirectory = EnvironmentHelper.ManagedProfilerDirectory;
    }
}
