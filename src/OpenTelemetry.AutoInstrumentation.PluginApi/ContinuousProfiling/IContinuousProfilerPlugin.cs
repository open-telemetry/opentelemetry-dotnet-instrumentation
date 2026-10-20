// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.PluginApi.ContinuousProfiling;

/// <summary>
/// Provides extension points for configuring continuous profiler.
/// </summary>
public interface IContinuousProfilerPlugin
{
    /// <summary>
    /// Get continuous profiler configuration.
    /// </summary>
    /// <returns>configuration</returns>
    ContinuousProfilerConfiguration GetFirstContinuousProfilerConfiguration();
}
