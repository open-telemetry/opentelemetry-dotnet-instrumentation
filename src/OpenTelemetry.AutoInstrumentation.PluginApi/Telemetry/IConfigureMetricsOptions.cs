// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.PluginApi.Telemetry;

/// <summary>
/// Adds extension point to overwrite any metrics options.
/// </summary>
/// <typeparam name="TOptions">Metrics option type.</typeparam>
public interface IConfigureMetricsOptions<in TOptions>
{
    /// <summary>
    /// Configures metrics options.
    /// </summary>
    /// <param name="options">Options instance</param>
    void ConfigureMetricsOptions(TOptions options);
}
