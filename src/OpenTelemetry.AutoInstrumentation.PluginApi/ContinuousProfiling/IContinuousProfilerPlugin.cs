// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.PluginApi.ContinuousProfiling;

/// <summary>
/// Provides the initial continuous profiler configuration.
/// </summary>
public interface IContinuousProfilerPlugin
{
    /// <summary>
    /// Gets the continuous profiler configuration used during initialization.
    /// </summary>
    /// <returns>The initial continuous profiler configuration.</returns>
    ContinuousProfilerConfiguration GetFirstContinuousProfilerConfiguration();
}
