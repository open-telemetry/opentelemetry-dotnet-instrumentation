// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests;

public class KafkaInstrumentationTests
{
    [Theory]
    [InlineData("abc")]
    [InlineData(int.MaxValue)]
    [InlineData(uint.MaxValue)]
    [InlineData(long.MaxValue)]
    [InlineData(ulong.MaxValue)]
    [InlineData(float.MaxValue)]
    [InlineData(double.MaxValue)]
    public void MessageKeyValueIsExtractedForBasicType(object value)
    {
        Assert.Equal(Convert.ToString(value, CultureInfo.InvariantCulture), KafkaInstrumentation.ExtractMessageKeyValue(value));
    }

    [Fact]
    public void MessageKeyValueIsExtractedForDecimal()
    {
        decimal input = decimal.MaxValue;
        Assert.Equal(Convert.ToString(input, CultureInfo.InvariantCulture), KafkaInstrumentation.ExtractMessageKeyValue(input));
    }

    [Fact]
    public void MessageKeyValueIsNotExtractedForUnrecognizedType()
    {
        var value = new byte[] { 1, 2, 3 };
        Assert.Null(KafkaInstrumentation.ExtractMessageKeyValue(value));
    }

    [Fact]
    public void BootstrapServersCache_AddAndTryGet_RoundTrips()
    {
        var instance = new object();
        BootstrapServersCache.Add(instance, "broker:9092");

        Assert.True(BootstrapServersCache.TryGet(instance, out var result));
        Assert.Equal("broker:9092", result);
    }

    [Fact]
    public void BootstrapServersCache_TryGet_ReturnsFalseForUnknownInstance()
    {
        var instance = new object();
        Assert.False(BootstrapServersCache.TryGet(instance, out var result));
        Assert.Null(result);
    }

    [Fact]
    public void BootstrapServersCache_Remove_ClearsEntry()
    {
        var instance = new object();
        BootstrapServersCache.Add(instance, "broker:9092");
        BootstrapServersCache.Remove(instance);

        Assert.False(BootstrapServersCache.TryGet(instance, out _));
    }

    [Fact]
    public void KafkaClusterIdCache_GetClusterId_ReturnsNullForNullOrEmpty()
    {
        Assert.Null(KafkaClusterIdCache.GetClusterId(null));
        Assert.Null(KafkaClusterIdCache.GetClusterId(string.Empty));
    }

    [Fact]
    public void KafkaClusterIdCache_GetClusterId_ReturnsNullWhenNotCached()
    {
        Assert.Null(KafkaClusterIdCache.GetClusterId("not-cached-host-unit-test:9999"));
    }

    [Fact]
    public void KafkaClusterIdCache_ScheduleFetch_NoOpsForEmpty()
    {
        KafkaClusterIdCache.ScheduleFetch(string.Empty);
    }

    [Fact]
    public void KafkaClusterIdCache_ScheduleFetch_DeduplicatesInFlightRequests()
    {
        const string key = "dedup-test-host-unit-test:9999";
        KafkaClusterIdCache.ScheduleFetch(key);
        KafkaClusterIdCache.ScheduleFetch(key);
    }
}
