// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Metrics;
using Xunit;

namespace OpenTelemetry.OutOfProcess.Forwarder.Tests.Metrics;

public class HistogramMetricPointFeaturesTests
{
    [Theory]
    [InlineData(HistogramMetricPointFeatures.None, 0)]
    [InlineData(HistogramMetricPointFeatures.MinAndMaxRecorded, 1)]
    [InlineData(HistogramMetricPointFeatures.BucketsRecorded, 2)]
    public void Features_HaveCorrectValues(HistogramMetricPointFeatures feature, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)feature);
    }

    [Fact]
    public void Features_CanBeCombined()
    {
        // Arrange
        var combined = HistogramMetricPointFeatures.MinAndMaxRecorded | HistogramMetricPointFeatures.BucketsRecorded;

        // Act & Assert
        Assert.Equal(3, (int)combined);
        Assert.True(combined.HasFlag(HistogramMetricPointFeatures.MinAndMaxRecorded));
        Assert.True(combined.HasFlag(HistogramMetricPointFeatures.BucketsRecorded));
    }

    [Fact]
    public void Features_FlagsAttribute_IsApplied()
    {
        // Verify that the enum has the Flags attribute
        var enumType = typeof(HistogramMetricPointFeatures);
        var flagsAttribute = enumType.GetCustomAttributes(typeof(FlagsAttribute), false);
        Assert.True(flagsAttribute.Length > 0);
    }

    [Fact]
    public void Features_AllValuesAreDefined()
    {
        var definedValues = Enum.GetValues<HistogramMetricPointFeatures>();

        Assert.Contains(HistogramMetricPointFeatures.None, definedValues);
        Assert.Contains(HistogramMetricPointFeatures.MinAndMaxRecorded, definedValues);
        Assert.Contains(HistogramMetricPointFeatures.BucketsRecorded, definedValues);
        Assert.Equal(3, definedValues.Length);
    }
}
