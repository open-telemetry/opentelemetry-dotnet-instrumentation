// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Configuration keys
/// </summary>
internal partial class ConfigurationKeys
{
    /// <summary>
    /// Configuration key to enable or disable assembly redirection in isolated AssemblyLoadContext.
    /// Default is true.
    /// </summary>
    public const string RedirectEnabled = "OTEL_DOTNET_AUTO_REDIRECT_ENABLED";
}
