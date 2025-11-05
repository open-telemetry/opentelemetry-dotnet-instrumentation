// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry;
using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class BatchProcessorConfigBase
{
    /// <summary>
    /// Gets or sets the delay interval (in milliseconds) between two consecutive exports.
    /// Value must be non-negative.
    /// If omitted or null, 5000 is used.
    /// </summary>
    [YamlMember(Alias = "schedule_delay")]
    public int ScheduleDelay { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the maximum allowed time (in milliseconds) to export data.
    /// Value must be non-negative. A value of 0 indicates no limit (infinity).
    /// If omitted or null, 30000 is used.
    /// </summary>
    [YamlMember(Alias = "export_timeout")]
    public int ExportTimeout { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the maximum queue size.
    /// Value must be positive.
    /// If omitted or null, 2048 is used.
    /// </summary>
    [YamlMember(Alias = "max_queue_size")]
    public int MaxQueueSize { get; set; } = 2048;

    /// <summary>
    /// Gets or sets the maximum batch size.
    /// Value must be positive.
    /// If omitted or null, 512 is used.
    /// </summary>
    [YamlMember(Alias = "max_export_batch_size")]
    public int MaxExportBatchSize { get; set; } = 512;

    public void CopyTo<T>(BatchExportProcessorOptions<T> options)
        where T : class
    {
        options.ScheduledDelayMilliseconds = ScheduleDelay;
        options.ExporterTimeoutMilliseconds = ExportTimeout;
        options.MaxQueueSize = MaxQueueSize;
        options.MaxExportBatchSize = MaxExportBatchSize;
    }
}
