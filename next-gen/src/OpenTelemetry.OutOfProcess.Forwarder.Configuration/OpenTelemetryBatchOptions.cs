// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Configuration;

namespace OpenTelemetry.Configuration;

/// <summary>
/// Contains OpenTelemetry batch options.
/// </summary>
public sealed class OpenTelemetryBatchOptions
{
    internal static OpenTelemetryBatchOptions ParseFromConfig(IConfigurationSection config)
    {
        Debug.Assert(config != null);

        if (!config.TryParseValue(nameof(MaxQueueSize), out int maxQueueSize))
        {
            maxQueueSize = DefaultMaxQueueSize;
        }

        if (!config.TryParseValue(nameof(MaxExportBatchSize), out int maxExportBatchSize))
        {
            maxExportBatchSize = DefaultMaxExportBatchSize;
        }

        if (!config.TryParseValue(nameof(ExportIntervalMilliseconds), out int exportIntervalMilliseconds))
        {
            exportIntervalMilliseconds = DefaultExportIntervalMilliseconds;
        }

        if (!config.TryParseValue(nameof(ExportTimeoutMilliseconds), out int exportTimeoutMilliseconds))
        {
            exportTimeoutMilliseconds = DefaultExportTimeoutMilliseconds;
        }

        return new(maxQueueSize, maxExportBatchSize, exportIntervalMilliseconds, exportTimeoutMilliseconds);
    }

    internal const int DefaultMaxQueueSize = 2048;
    internal const int DefaultMaxExportBatchSize = 512;
    internal const int DefaultExportIntervalMilliseconds = 5000;
    internal const int DefaultExportTimeoutMilliseconds = 30000;

    internal OpenTelemetryBatchOptions(
        int maxQueueSize,
        int maxExportBatchSize,
        int exportIntervalMilliseconds,
        int exportTimeoutMilliseconds)
    {
        MaxQueueSize = maxQueueSize;
        MaxExportBatchSize = maxExportBatchSize;
        ExportIntervalMilliseconds = exportIntervalMilliseconds;
        ExportTimeoutMilliseconds = exportTimeoutMilliseconds;
    }

    /// <summary>
    /// Gets the maximum queue size. The queue drops the data if the maximum size is reached. The default value is 2048.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int MaxQueueSize { get; }

    /// <summary>
    /// Gets the maximum batch size of every export. It must be smaller or equal to MaxQueueLength. The default value is 512.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int MaxExportBatchSize { get; }

    /// <summary>
    /// Gets the delay interval (in milliseconds) between two consecutive exports. The default value is 5000.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int ExportIntervalMilliseconds { get; }

    /// <summary>
    /// Gets the timeout (in milliseconds) after which the export is cancelled. The default value is 30000.
    /// </summary>
    /// <remarks>Note: Set to <c>-1</c> to disable timeout.</remarks>
    [Range(1, int.MaxValue)]
    public int ExportTimeoutMilliseconds { get; }

    internal BatchExportProcessorOptions ToOTelBatchOptions()
        => new(MaxQueueSize, MaxExportBatchSize, ExportIntervalMilliseconds, ExportTimeoutMilliseconds);
}
