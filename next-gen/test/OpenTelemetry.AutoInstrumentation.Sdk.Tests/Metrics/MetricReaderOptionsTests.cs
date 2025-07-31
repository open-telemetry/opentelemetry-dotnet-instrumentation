// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Metrics;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests.Metrics;

public class MetricReaderOptionsTests
{
    [Fact]
    public void Constructor_WithDefaults_SetsExpectedValues()
    {
        // Act
        var options = new MetricReaderOptions();

        // Assert
        Assert.Equal(AggregationTemporality.Cumulative, options.AggregationTemporalityPreference);
        Assert.Equal(30000, options.ExportTimeoutMilliseconds);
    }

    [Theory]
    [InlineData(AggregationTemporality.Delta)]
    [InlineData(AggregationTemporality.Cumulative)]
    public void Constructor_WithSpecificTemporality_SetsCorrectly(AggregationTemporality temporality)
    {
        // Act
        var options = new MetricReaderOptions(temporality);

        // Assert
        Assert.Equal(temporality, options.AggregationTemporalityPreference);
        Assert.Equal(30000, options.ExportTimeoutMilliseconds);
    }

    [Fact]
    public void Constructor_WithUnspecifiedTemporality_DefaultsToCumulative()
    {
        // Act
        var options = new MetricReaderOptions(AggregationTemporality.Unspecified);

        // Assert
        Assert.Equal(AggregationTemporality.Cumulative, options.AggregationTemporalityPreference);
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(5000)]
    [InlineData(60000)]
    [InlineData(-1)] // Disable timeout
    public void Constructor_WithValidTimeout_SetsCorrectly(int timeout)
    {
        // Act
        var options = new MetricReaderOptions(exportTimeoutMilliseconds: timeout);

        // Assert
        Assert.Equal(timeout, options.ExportTimeoutMilliseconds);
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(-100)]
    public void Constructor_WithInvalidTimeout_ThrowsArgumentOutOfRangeException(int invalidTimeout)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MetricReaderOptions(exportTimeoutMilliseconds: invalidTimeout));
    }

    [Fact]
    public void Constructor_WithBothParameters_SetsCorrectly()
    {
        // Arrange
        var temporality = AggregationTemporality.Delta;
        var timeout = 15000;

        // Act
        var options = new MetricReaderOptions(temporality, timeout);

        // Assert
        Assert.Equal(temporality, options.AggregationTemporalityPreference);
        Assert.Equal(timeout, options.ExportTimeoutMilliseconds);
    }

    [Fact]
    public void DefaultExportTimeoutMilliseconds_HasCorrectValue()
    {
        // This test ensures the default timeout constant is accessible and correct
        var options = new MetricReaderOptions();
        Assert.Equal(30000, options.ExportTimeoutMilliseconds);
    }
}
