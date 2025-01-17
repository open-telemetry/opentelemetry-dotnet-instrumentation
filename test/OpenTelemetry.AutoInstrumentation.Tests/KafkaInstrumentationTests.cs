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
}
