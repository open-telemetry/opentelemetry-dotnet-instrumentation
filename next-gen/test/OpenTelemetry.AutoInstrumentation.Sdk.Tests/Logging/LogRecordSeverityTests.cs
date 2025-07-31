// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Logging;

using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests.Logging;

public sealed class LogRecordSeverityTests
{
    [Fact]
    public void AllSeverityValues_HaveCorrectNumericValues()
    {
        // Assert - Verify the numeric values match the OpenTelemetry specification
        Assert.Equal(0, (int)LogRecordSeverity.Unspecified);
        Assert.Equal(1, (int)LogRecordSeverity.Trace);
        Assert.Equal(2, (int)LogRecordSeverity.Trace2);
        Assert.Equal(3, (int)LogRecordSeverity.Trace3);
        Assert.Equal(4, (int)LogRecordSeverity.Trace4);
        Assert.Equal(5, (int)LogRecordSeverity.Debug);
        Assert.Equal(6, (int)LogRecordSeverity.Debug2);
        Assert.Equal(7, (int)LogRecordSeverity.Debug3);
        Assert.Equal(8, (int)LogRecordSeverity.Debug4);
        Assert.Equal(9, (int)LogRecordSeverity.Info);
        Assert.Equal(10, (int)LogRecordSeverity.Info2);
        Assert.Equal(11, (int)LogRecordSeverity.Info3);
        Assert.Equal(12, (int)LogRecordSeverity.Info4);
        Assert.Equal(13, (int)LogRecordSeverity.Warn);
        Assert.Equal(14, (int)LogRecordSeverity.Warn2);
        Assert.Equal(15, (int)LogRecordSeverity.Warn3);
        Assert.Equal(16, (int)LogRecordSeverity.Warn4);
        Assert.Equal(17, (int)LogRecordSeverity.Error);
        Assert.Equal(18, (int)LogRecordSeverity.Error2);
        Assert.Equal(19, (int)LogRecordSeverity.Error3);
        Assert.Equal(20, (int)LogRecordSeverity.Error4);
        Assert.Equal(21, (int)LogRecordSeverity.Fatal);
        Assert.Equal(22, (int)LogRecordSeverity.Fatal2);
        Assert.Equal(23, (int)LogRecordSeverity.Fatal3);
        Assert.Equal(24, (int)LogRecordSeverity.Fatal4);
    }

    [Fact]
    public void SeverityLevels_AreInAscendingOrder()
    {
        // Arrange
        var severities = new[]
        {
            LogRecordSeverity.Unspecified,
            LogRecordSeverity.Trace,
            LogRecordSeverity.Trace2,
            LogRecordSeverity.Trace3,
            LogRecordSeverity.Trace4,
            LogRecordSeverity.Debug,
            LogRecordSeverity.Debug2,
            LogRecordSeverity.Debug3,
            LogRecordSeverity.Debug4,
            LogRecordSeverity.Info,
            LogRecordSeverity.Info2,
            LogRecordSeverity.Info3,
            LogRecordSeverity.Info4,
            LogRecordSeverity.Warn,
            LogRecordSeverity.Warn2,
            LogRecordSeverity.Warn3,
            LogRecordSeverity.Warn4,
            LogRecordSeverity.Error,
            LogRecordSeverity.Error2,
            LogRecordSeverity.Error3,
            LogRecordSeverity.Error4,
            LogRecordSeverity.Fatal,
            LogRecordSeverity.Fatal2,
            LogRecordSeverity.Fatal3,
            LogRecordSeverity.Fatal4
        };

        // Act & Assert
        for (int i = 1; i < severities.Length; i++)
        {
            Assert.True(
                (int)severities[i] > (int)severities[i - 1],
                $"{severities[i]} should be greater than {severities[i - 1]}");
        }
    }

    [Fact]
    public void SeverityComparison_WorksCorrectly()
    {
        // Act & Assert
        Assert.True(LogRecordSeverity.Debug > LogRecordSeverity.Trace);
        Assert.True(LogRecordSeverity.Info > LogRecordSeverity.Debug);
        Assert.True(LogRecordSeverity.Warn > LogRecordSeverity.Info);
        Assert.True(LogRecordSeverity.Error > LogRecordSeverity.Warn);
        Assert.True(LogRecordSeverity.Fatal > LogRecordSeverity.Error);
    }

    [Fact]
    public void SeverityEquality_WorksCorrectly()
    {
        // Act & Assert
        Assert.Equal(LogRecordSeverity.Info, LogRecordSeverity.Info);
        Assert.NotEqual(LogRecordSeverity.Info, LogRecordSeverity.Warn);
    }

    [Theory]
    [InlineData(LogRecordSeverity.Unspecified)]
    [InlineData(LogRecordSeverity.Trace)]
    [InlineData(LogRecordSeverity.Debug)]
    [InlineData(LogRecordSeverity.Info)]
    [InlineData(LogRecordSeverity.Warn)]
    [InlineData(LogRecordSeverity.Error)]
    [InlineData(LogRecordSeverity.Fatal)]
    public void AllMainSeverityLevels_CanBeUsed(LogRecordSeverity severity)
    {
        // Act & Assert - Should not throw
        var result = severity.ToString();
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void EnumValues_CanBeConvertedToString()
    {
        // Act & Assert
        Assert.Equal("Unspecified", LogRecordSeverity.Unspecified.ToString());
        Assert.Equal("Trace", LogRecordSeverity.Trace.ToString());
        Assert.Equal("Debug", LogRecordSeverity.Debug.ToString());
        Assert.Equal("Info", LogRecordSeverity.Info.ToString());
        Assert.Equal("Warn", LogRecordSeverity.Warn.ToString());
        Assert.Equal("Error", LogRecordSeverity.Error.ToString());
        Assert.Equal("Fatal", LogRecordSeverity.Fatal.ToString());
    }

    [Fact]
    public void EnumValues_CanBeParsedFromString()
    {
        // Act & Assert
        Assert.True(Enum.TryParse<LogRecordSeverity>("Info", out var parsedSeverity));
        Assert.Equal(LogRecordSeverity.Info, parsedSeverity);

        Assert.True(Enum.TryParse<LogRecordSeverity>("Error", out parsedSeverity));
        Assert.Equal(LogRecordSeverity.Error, parsedSeverity);

        Assert.False(Enum.TryParse<LogRecordSeverity>("InvalidSeverity", out _));
    }

    [Fact]
    public void EnumValues_CanBeUsedInSwitch()
    {
        // Arrange & Act & Assert
        foreach (LogRecordSeverity severity in Enum.GetValues<LogRecordSeverity>())
        {
            var result = severity switch
            {
                LogRecordSeverity.Unspecified => "unspecified",
                LogRecordSeverity.Trace or LogRecordSeverity.Trace2 or LogRecordSeverity.Trace3 or LogRecordSeverity.Trace4 => "trace",
                LogRecordSeverity.Debug or LogRecordSeverity.Debug2 or LogRecordSeverity.Debug3 or LogRecordSeverity.Debug4 => "debug",
                LogRecordSeverity.Info or LogRecordSeverity.Info2 or LogRecordSeverity.Info3 or LogRecordSeverity.Info4 => "info",
                LogRecordSeverity.Warn or LogRecordSeverity.Warn2 or LogRecordSeverity.Warn3 or LogRecordSeverity.Warn4 => "warn",
                LogRecordSeverity.Error or LogRecordSeverity.Error2 or LogRecordSeverity.Error3 or LogRecordSeverity.Error4 => "error",
                LogRecordSeverity.Fatal or LogRecordSeverity.Fatal2 or LogRecordSeverity.Fatal3 or LogRecordSeverity.Fatal4 => "fatal",
                _ => "unknown"
            };

            Assert.NotEqual("unknown", result);
        }
    }

    [Fact]
    public void AllDefinedEnumValues_ExistAndAreValid()
    {
        // Arrange
        var expectedValues = new[]
        {
            LogRecordSeverity.Unspecified, LogRecordSeverity.Trace, LogRecordSeverity.Trace2, LogRecordSeverity.Trace3, LogRecordSeverity.Trace4,
            LogRecordSeverity.Debug, LogRecordSeverity.Debug2, LogRecordSeverity.Debug3, LogRecordSeverity.Debug4,
            LogRecordSeverity.Info, LogRecordSeverity.Info2, LogRecordSeverity.Info3, LogRecordSeverity.Info4,
            LogRecordSeverity.Warn, LogRecordSeverity.Warn2, LogRecordSeverity.Warn3, LogRecordSeverity.Warn4,
            LogRecordSeverity.Error, LogRecordSeverity.Error2, LogRecordSeverity.Error3, LogRecordSeverity.Error4,
            LogRecordSeverity.Fatal, LogRecordSeverity.Fatal2, LogRecordSeverity.Fatal3, LogRecordSeverity.Fatal4
        };

        // Act
        var actualValues = Enum.GetValues<LogRecordSeverity>();

        // Assert
        Assert.Equal(expectedValues.Length, actualValues.Length);

        foreach (var expectedValue in expectedValues)
        {
            Assert.Contains(expectedValue, actualValues);
            Assert.True(Enum.IsDefined(expectedValue));
        }
    }
}
