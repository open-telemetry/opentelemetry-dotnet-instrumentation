// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Xunit;

namespace OpenTelemetry.Configuration.Tests;

public sealed class OpenTelemetryBatchOptionsTests
{
    [Fact]
    public void Constructor_WithValidParameters_SetsProperties()
    {
        // Arrange
        const int maxQueueSize = 1024;
        const int maxExportBatchSize = 256;
        const int exportIntervalMilliseconds = 5000;
        const int exportTimeoutMilliseconds = 30000;

        // Act
        var options = new OpenTelemetryBatchOptions(
            maxQueueSize,
            maxExportBatchSize,
            exportIntervalMilliseconds,
            exportTimeoutMilliseconds);

        // Assert
        Assert.Equal(maxQueueSize, options.MaxQueueSize);
        Assert.Equal(maxExportBatchSize, options.MaxExportBatchSize);
        Assert.Equal(exportIntervalMilliseconds, options.ExportIntervalMilliseconds);
        Assert.Equal(exportTimeoutMilliseconds, options.ExportTimeoutMilliseconds);
    }

    [Fact]
    public void Constructor_WithMinimumValues_SetsProperties()
    {
        // Arrange
        const int maxQueueSize = 1;
        const int maxExportBatchSize = 1;
        const int exportIntervalMilliseconds = 100;
        const int exportTimeoutMilliseconds = 1000;

        // Act
        var options = new OpenTelemetryBatchOptions(
            maxQueueSize,
            maxExportBatchSize,
            exportIntervalMilliseconds,
            exportTimeoutMilliseconds);

        // Assert
        Assert.Equal(maxQueueSize, options.MaxQueueSize);
        Assert.Equal(maxExportBatchSize, options.MaxExportBatchSize);
        Assert.Equal(exportIntervalMilliseconds, options.ExportIntervalMilliseconds);
        Assert.Equal(exportTimeoutMilliseconds, options.ExportTimeoutMilliseconds);
    }

    [Fact]
    public void Constructor_WithLargeValues_SetsProperties()
    {
        // Arrange
        const int maxQueueSize = 10000;
        const int maxExportBatchSize = 5000;
        const int exportIntervalMilliseconds = 60000;
        const int exportTimeoutMilliseconds = 120000;

        // Act
        var options = new OpenTelemetryBatchOptions(
            maxQueueSize,
            maxExportBatchSize,
            exportIntervalMilliseconds,
            exportTimeoutMilliseconds);

        // Assert
        Assert.Equal(maxQueueSize, options.MaxQueueSize);
        Assert.Equal(maxExportBatchSize, options.MaxExportBatchSize);
        Assert.Equal(exportIntervalMilliseconds, options.ExportIntervalMilliseconds);
        Assert.Equal(exportTimeoutMilliseconds, options.ExportTimeoutMilliseconds);
    }

    [Theory]
    [InlineData(512, 128, 2000, 15000)]
    [InlineData(2048, 512, 10000, 60000)]
    [InlineData(4096, 1024, 1000, 5000)]
    public void Constructor_WithVariousValidCombinations_SetsPropertiesCorrectly(
        int maxQueueSize,
        int maxExportBatchSize,
        int exportIntervalMilliseconds,
        int exportTimeoutMilliseconds)
    {
        // Act
        var options = new OpenTelemetryBatchOptions(
            maxQueueSize,
            maxExportBatchSize,
            exportIntervalMilliseconds,
            exportTimeoutMilliseconds);

        // Assert
        Assert.Equal(maxQueueSize, options.MaxQueueSize);
        Assert.Equal(maxExportBatchSize, options.MaxExportBatchSize);
        Assert.Equal(exportIntervalMilliseconds, options.ExportIntervalMilliseconds);
        Assert.Equal(exportTimeoutMilliseconds, options.ExportTimeoutMilliseconds);
    }
}
