// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using FluentAssertions;
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
        KafkaInstrumentation.ExtractMessageKeyValue(value).Should().Be(Convert.ToString(value, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void MessageKeyValueIsExtractedForDecimal()
    {
        decimal input = decimal.MaxValue;
        KafkaInstrumentation.ExtractMessageKeyValue(input).Should().Be(Convert.ToString(input, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void MessageKeyValueIsNotExtractedForUnrecognizedType()
    {
        var value = new byte[] { 1, 2, 3 };
        KafkaInstrumentation.ExtractMessageKeyValue(value).Should().BeNull();
    }
}
