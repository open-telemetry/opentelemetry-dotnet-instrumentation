// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Configuration keys
/// </summary>
internal partial class ConfigurationKeys
{
    /// <summary>
    /// Configuration key for the OpenTelemetry .NET Automatic Instrumentation home directory.
    /// Default is empty.
    /// </summary>
    public const string TracerHome = "OTEL_DOTNET_AUTO_HOME";
}
