// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Models;

internal class BatchProcessorConfig
{
    public BatchProcessorConfig()
    {
    }

    public BatchProcessorConfig(
        int? scheduleDelay = null,
        int? exportTimeout = null,
        int? maxQueueSize = null,
        int? maxExportBatchSize = null)
    {
        if (scheduleDelay is not null)
        {
            ScheduleDelay = scheduleDelay.Value;
        }

        if (exportTimeout is not null)
        {
            ExportTimeout = exportTimeout.Value;
        }

        if (maxQueueSize is not null)
        {
            MaxQueueSize = maxQueueSize.Value;
        }

        if (maxExportBatchSize is not null)
        {
            MaxExportBatchSize = maxExportBatchSize.Value;
        }
    }

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

    /// <summary>
    /// Gets or sets the exporters.
    /// </summary>
    [YamlMember(Alias = "exporter")]
    public ExporterConfig? Exporter { get; set; }

    public BatchExportProcessorOptions<Activity> ToBatchExportProcessorOptions()
    {
        return new BatchExportProcessorOptions<Activity>
        {
            ScheduledDelayMilliseconds = this.ScheduleDelay,
            ExporterTimeoutMilliseconds = this.ExportTimeout,
            MaxQueueSize = this.MaxQueueSize,
            MaxExportBatchSize = this.MaxExportBatchSize
        };
    }
}
