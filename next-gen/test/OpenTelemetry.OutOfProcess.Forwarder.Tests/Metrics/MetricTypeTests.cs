// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Metrics;
using Xunit;

namespace OpenTelemetry.OutOfProcess.Forwarder.Tests.Metrics;

public class MetricTypeTests
{
    [Theory]
    [InlineData(MetricType.LongSum, 0x1a)]
    [InlineData(MetricType.DoubleSum, 0x1d)]
    [InlineData(MetricType.LongGauge, 0x2a)]
    [InlineData(MetricType.DoubleGauge, 0x2d)]
    [InlineData(MetricType.Summary, 0x30)]
    [InlineData(MetricType.Histogram, 0x40)]
    [InlineData(MetricType.ExponentialHistogram, 0x50)]
    [InlineData(MetricType.LongSumNonMonotonic, 0x8a)]
    [InlineData(MetricType.DoubleSumNonMonotonic, 0x8d)]
    public void MetricType_HasCorrectByteValue(MetricType metricType, byte expectedValue)
    {
        Assert.Equal(expectedValue, (byte)metricType);
    }

    [Theory]
    [InlineData(MetricType.LongSum, false)]
    [InlineData(MetricType.DoubleSum, false)]
    [InlineData(MetricType.LongGauge, false)]
    [InlineData(MetricType.DoubleGauge, false)]
    [InlineData(MetricType.Summary, false)]
    [InlineData(MetricType.Histogram, false)]
    [InlineData(MetricType.ExponentialHistogram, false)]
    [InlineData(MetricType.LongSumNonMonotonic, true)]
    [InlineData(MetricType.DoubleSumNonMonotonic, true)]
    public void MetricType_NonMonotonicFlag_IsCorrect(MetricType metricType, bool expectedNonMonotonic)
    {
        // Test the bit manipulation logic used in Metric.IsSumNonMonotonic
        // The correct implementation uses: ((byte)MetricType & 0x80) != 0
        bool isNonMonotonic = ((byte)metricType & 0x80) != 0;
        Assert.Equal(expectedNonMonotonic, isNonMonotonic);
    }

    [Theory]
    [InlineData(MetricType.LongSum, false)]
    [InlineData(MetricType.DoubleSum, true)]
    [InlineData(MetricType.LongGauge, false)]
    [InlineData(MetricType.DoubleGauge, true)]
    [InlineData(MetricType.LongSumNonMonotonic, false)]
    [InlineData(MetricType.DoubleSumNonMonotonic, true)]
    public void MetricType_FloatingPointFlag_IsCorrect(MetricType metricType, bool expectedFloatingPoint)
    {
        // Test the bit manipulation logic used in Metric.IsFloatingPoint
        // The correct implementation uses: ((byte)MetricType & 0x0c) == 0x0c
        bool isFloatingPoint = ((byte)metricType & 0x0c) == 0x0c;
        Assert.Equal(expectedFloatingPoint, isFloatingPoint);
    }

    [Fact]
    public void MetricType_Unknown_HasZeroValue()
    {
        Assert.Equal(0, (byte)MetricType.Unknown);
    }
}
