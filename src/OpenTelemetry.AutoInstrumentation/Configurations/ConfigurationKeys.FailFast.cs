// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Configuration keys
/// </summary>
internal partial class ConfigurationKeys
{
    /// <summary>
    /// Configuration key to set fail fast behavior.
    /// Default is false.
    /// </summary>
    public const string FailFast = "OTEL_DOTNET_AUTO_FAIL_FAST_ENABLED";
}
