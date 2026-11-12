// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.SelfDiagnostics;

namespace OpenTelemetry.AutoInstrumentation.PluginApi.Telemetry;

/// <summary>
/// Adds extension point to overwrite SDK self-diagnostics options.
/// </summary>
public interface IConfigureSelfDiagnosticsOptions
{
    /// <summary>
    /// Configures self-diagnostics options.
    /// </summary>
    /// <param name="options">The <see cref="SelfDiagnosticsOptions"/> instance</param>
    void ConfigureSelfDiagnosticsOptions(SelfDiagnosticsOptions options);
}
