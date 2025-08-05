// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry;

/// <summary>
/// Contains batch export processor options.
/// </summary>
public sealed class BatchExportProcessorOptions
{
    private const int DefaultMaxQueueSize = 2048;
    private const int DefaultMaxExportBatchSize = 512;
    private const int DefaultExportIntervalMilliseconds = 5000;
    private const int DefaultExportTimeoutMilliseconds = 30000;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchExportProcessorOptions"/> class.
    /// </summary>
    /// <param name="maxQueueSize">Max queue size.</param>
    /// <param name="maxExportBatchSize">Max batch size.</param>
    /// <param name="exportIntervalMilliseconds">Export interval in milliseconds.</param>
    /// <param name="exportTimeoutMilliseconds">Export timeout in milliseconds.</param>
    public BatchExportProcessorOptions(
        int maxQueueSize = DefaultMaxQueueSize,
        int maxExportBatchSize = DefaultMaxExportBatchSize,
        int exportIntervalMilliseconds = DefaultExportIntervalMilliseconds,
        int exportTimeoutMilliseconds = DefaultExportTimeoutMilliseconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxQueueSize, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxExportBatchSize, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(exportIntervalMilliseconds, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(exportTimeoutMilliseconds, -1);

        if (maxExportBatchSize > maxQueueSize)
        {
            throw new ArgumentException($"The value supplied for {nameof(maxExportBatchSize)} must be less than or equal to the value supplied for {maxQueueSize}");
        }

        MaxQueueSize = maxQueueSize;
        MaxExportBatchSize = maxExportBatchSize;
        ExportIntervalMilliseconds = exportIntervalMilliseconds;
        ExportTimeoutMilliseconds = exportTimeoutMilliseconds;
    }

    /// <summary>
    /// Gets the maximum queue size. The queue drops the data if the maximum size is reached. The default value is 2048.
    /// </summary>
    public int MaxQueueSize { get; }

    /// <summary>
    /// Gets the maximum batch size of every export. It must be smaller or equal to MaxQueueLength. The default value is 512.
    /// </summary>
    public int MaxExportBatchSize { get; }

    /// <summary>
    /// Gets the delay interval (in milliseconds) between two consecutive exports. The default value is 5000.
    /// </summary>
    public int ExportIntervalMilliseconds { get; }

    /// <summary>
    /// Gets the timeout (in milliseconds) after which the export is cancelled. The default value is 30000.
    /// </summary>
    /// <remarks>Note: Set to <c>-1</c> to disable timeout.</remarks>
    public int ExportTimeoutMilliseconds { get; }
}
