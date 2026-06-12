// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.PluginApi;

/// <summary>
/// General Plugin API
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Is called when auto instrumentation setup begins.
    /// </summary>
    void Initializing();

    /// <summary>
    /// Is called when auto instrumentation setup finalized.
    /// </summary>
    void Initialized();
}
