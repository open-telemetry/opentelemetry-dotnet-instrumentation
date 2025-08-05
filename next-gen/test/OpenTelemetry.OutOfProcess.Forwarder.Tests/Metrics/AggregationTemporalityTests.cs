// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Metrics;
using Xunit;

namespace OpenTelemetry.OutOfProcess.Forwarder.Tests.Metrics;

public class AggregationTemporalityTests
{
    [Theory]
    [InlineData(AggregationTemporality.Unspecified, 0)]
    [InlineData(AggregationTemporality.Delta, 1)]
    [InlineData(AggregationTemporality.Cumulative, 2)]
    public void AggregationTemporality_HasCorrectValue(AggregationTemporality temporality, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)temporality);
    }

    [Fact]
    public void AggregationTemporality_AllValuesAreDefined()
    {
        var definedValues = Enum.GetValues<AggregationTemporality>();

        Assert.Contains(AggregationTemporality.Unspecified, definedValues);
        Assert.Contains(AggregationTemporality.Delta, definedValues);
        Assert.Contains(AggregationTemporality.Cumulative, definedValues);
        Assert.Equal(3, definedValues.Length);
    }
}
