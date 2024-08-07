// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET6_0_OR_GREATER

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal class ContinuousProfilerSettings : Settings
{
    /// <summary>
    /// Gets a value indicating whether the thread sampling is enabled.
    /// Default is <c>false</c>.
    /// </summary>
    public bool ThreadSamplingEnabled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the allocation sampling is enabled.
    /// Default is <c>false</c>.
    /// </summary>
    public bool AllocationSamplingEnabled { get; private set; }

    /// <summary>
    /// Gets a value of thread sampling interval.
    /// Default is <c>1000</c>.
    /// </summary>
    public uint ThreadSamplingInterval { get; private set; }

    /// <summary>
    /// Gets a value of mam memory samples per minute.
    /// Default is <c>200</c>.
    /// </summary>
    public uint MaxMemorySamplesPerMinute { get; private set; }

    /// <summary>
    /// Gets a value of export interval.
    /// Default is <c>500ms</c>.
    /// </summary>
    public TimeSpan ExportInterval { get; private set; }

    /// <summary>
    /// Gets a value of export timeout.
    /// Default is <c>5000ms</c>.
    /// </summary>
    public TimeSpan ExportTimeout { get; private set; }

    protected override void OnLoad(Configuration configuration)
    {
        ThreadSamplingEnabled = configuration.GetBool(ConfigurationKeys.ContinuousProfiler.ThreadSamplingEnabled) ?? false;
        AllocationSamplingEnabled = configuration.GetBool(ConfigurationKeys.ContinuousProfiler.AllocationSamplingEnabled) ?? false;
        ThreadSamplingInterval = 1000u;
        MaxMemorySamplesPerMinute = 200u;
        ExportInterval = TimeSpan.FromMilliseconds(500);
        ExportTimeout = TimeSpan.FromMilliseconds(5000);
    }
}
#endif
