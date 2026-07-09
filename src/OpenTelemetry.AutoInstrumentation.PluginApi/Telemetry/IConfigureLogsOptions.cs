// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.PluginApi.Telemetry;

/// <summary>
/// Adds extension point to overwrite any logs options.
/// </summary>
/// <typeparam name="TOptions">Logs option type.</typeparam>
public interface IConfigureLogsOptions<in TOptions>
{
    /// <summary>
    /// Configures logs options.
    /// </summary>
    /// <param name="options">Options instance</param>
    void ConfigureLogsOptions(TOptions options);
}
