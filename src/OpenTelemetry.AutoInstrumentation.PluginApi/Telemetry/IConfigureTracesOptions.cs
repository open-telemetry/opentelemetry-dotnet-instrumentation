// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.PluginApi.Telemetry;

/// <summary>
/// Adds extension point to overwrite any traces options.
/// </summary>
/// <typeparam name="TOptions">Tracesd option type.</typeparam>
public interface IConfigureTracesOptions<in TOptions>
{
    /// <summary>
    /// Configures traces options.
    /// </summary>
    /// <param name="options">Options instance</param>
    void ConfigureTracesOptions(TOptions options);
}
