// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.PluginApi.SelectiveSampling;

/// <summary>
/// Provides extension points for configuring selective sampler.
/// </summary>
public interface ISelectiveSamplerPlugin
{
    /// <summary>
    /// Get selective sampler configuration.
    /// </summary>
    /// <returns>configuration</returns>
    SelectiveSamplerConfiguration? GetFirstSelectiveSamplingConfiguration();
}
