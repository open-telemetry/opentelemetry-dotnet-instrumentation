// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Metrics;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests.Metrics;

public class PeriodicExportingMetricReaderOptionsTests
{
    [Fact]
    public void Constructor_WithDefaults_SetsExpectedValues()
    {
        // Act
        var options = new PeriodicExportingMetricReaderOptions();

        // Assert
        Assert.Equal(AggregationTemporality.Cumulative, options.AggregationTemporalityPreference);
        Assert.Equal(5000, options.ExportIntervalMilliseconds);
        Assert.Equal(30000, options.ExportTimeoutMilliseconds);
    }

    [Theory]
    [InlineData(AggregationTemporality.Delta)]
    [InlineData(AggregationTemporality.Cumulative)]
    public void Constructor_WithTemporality_SetsCorrectly(AggregationTemporality temporality)
    {
        // Act
        var options = new PeriodicExportingMetricReaderOptions(temporality);

        // Assert
        Assert.Equal(temporality, options.AggregationTemporalityPreference);
        Assert.Equal(5000, options.ExportIntervalMilliseconds);
        Assert.Equal(30000, options.ExportTimeoutMilliseconds);
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(2000)]
    [InlineData(10000)]
    public void Constructor_WithValidExportInterval_SetsCorrectly(int interval)
    {
        // Act
        var options = new PeriodicExportingMetricReaderOptions(exportIntervalMilliseconds: interval);

        // Assert
        Assert.Equal(interval, options.ExportIntervalMilliseconds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithInvalidExportInterval_ThrowsArgumentOutOfRangeException(int invalidInterval)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PeriodicExportingMetricReaderOptions(exportIntervalMilliseconds: invalidInterval));
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(5000)]
    [InlineData(60000)]
    [InlineData(-1)] // Disable timeout
    public void Constructor_WithValidExportTimeout_SetsCorrectly(int timeout)
    {
        // Act
        var options = new PeriodicExportingMetricReaderOptions(exportTimeoutMilliseconds: timeout);

        // Assert
        Assert.Equal(timeout, options.ExportTimeoutMilliseconds);
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(-100)]
    public void Constructor_WithInvalidExportTimeout_ThrowsArgumentOutOfRangeException(int invalidTimeout)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PeriodicExportingMetricReaderOptions(exportTimeoutMilliseconds: invalidTimeout));
    }

    [Fact]
    public void Constructor_WithAllParameters_SetsCorrectly()
    {
        // Arrange
        var temporality = AggregationTemporality.Delta;
        var interval = 2000;
        var timeout = 15000;

        // Act
        var options = new PeriodicExportingMetricReaderOptions(temporality, interval, timeout);

        // Assert
        Assert.Equal(temporality, options.AggregationTemporalityPreference);
        Assert.Equal(interval, options.ExportIntervalMilliseconds);
        Assert.Equal(timeout, options.ExportTimeoutMilliseconds);
    }

    [Fact]
    public void InheritsFromMetricReaderOptions()
    {
        // Act
        var options = new PeriodicExportingMetricReaderOptions();

        // Assert
        Assert.IsAssignableFrom<MetricReaderOptions>(options);
    }
}
