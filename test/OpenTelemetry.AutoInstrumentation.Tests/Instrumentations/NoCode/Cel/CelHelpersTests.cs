// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.Cel;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Instrumentations.NoCode.Cel;

public class CelHelpersTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void IsTrue_WithBoolean_ReturnsCorrectValue(bool value, bool expected)
    {
        var result = CelHelpers.IsTrue(value);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(-1, true)]
    [InlineData(100, true)]
    [InlineData(-100, true)]
    public void IsTrue_WithInteger_ReturnsCorrectValue(int value, bool expected)
    {
        var result = CelHelpers.IsTrue(value);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0L, false)]
    [InlineData(1L, true)]
    [InlineData(-1L, true)]
    [InlineData(100L, true)]
    public void IsTrue_WithLong_ReturnsCorrectValue(long value, bool expected)
    {
        var result = CelHelpers.IsTrue(value);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0.0, false)]
    [InlineData(0.1, true)]
    [InlineData(-0.1, true)]
    [InlineData(100.5, true)]
    public void IsTrue_WithDouble_ReturnsCorrectValue(double value, bool expected)
    {
        var result = CelHelpers.IsTrue(value);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0.0f, false)]
    [InlineData(0.1f, true)]
    [InlineData(-0.1f, true)]
    [InlineData(100.5f, true)]
    public void IsTrue_WithFloat_ReturnsCorrectValue(float value, bool expected)
    {
        var result = CelHelpers.IsTrue(value);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("hello", true)]
    [InlineData(" ", true)]
    [InlineData("false", true)]
    public void IsTrue_WithString_ReturnsCorrectValue(string value, bool expected)
    {
        var result = CelHelpers.IsTrue(value);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsTrue_WithNull_ReturnsFalse()
    {
        var result = CelHelpers.IsTrue(null);

        Assert.False(result);
    }

    [Fact]
    public void IsTrue_WithObject_ReturnsTrue()
    {
        var obj = new { Property = "value" };
        var result = CelHelpers.IsTrue(obj);

        Assert.True(result);
    }

    [Theory]
    [InlineData(0.0000001, true)]
    [InlineData(-0.0000001, true)]
    public void IsTrue_WithDoubleCloseToZero_ReturnsTrue(double value, bool expected)
    {
        var result = CelHelpers.IsTrue(value);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0.0000001f, true)]
    [InlineData(-0.0000001f, true)]
    public void IsTrue_WithFloatCloseToZero_ReturnsTrue(float value, bool expected)
    {
        var result = CelHelpers.IsTrue(value);

        Assert.Equal(expected, result);
    }
}
