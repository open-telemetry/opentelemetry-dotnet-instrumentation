// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Instrumentations.NLog.Bridge;
using OpenTelemetry.Trace;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests;

/// <summary>
/// Unit tests for NLog instrumentation functionality.
/// These tests verify that NLog log levels are correctly mapped to OpenTelemetry severity levels
/// and that the NLog bridge functions properly.
/// </summary>
public class NLogTests
{
    // TODO: Remove when Logs Api is made public in non-rc builds.
    private static readonly Type OpenTelemetryLogSeverityType = typeof(Tracer).Assembly.GetType("OpenTelemetry.Logs.LogRecordSeverity")!;

    /// <summary>
    /// Provides test data for NLog level mapping tests.
    /// This includes all standard NLog levels and their expected OpenTelemetry severity mappings.
    /// </summary>
    /// <returns>Theory data containing NLog level ordinals and expected OpenTelemetry severity values.</returns>
    public static TheoryData<int, int> GetLevelMappingData()
    {
        var theoryData = new TheoryData<int, int>
        {
            // NLog.LogLevel.Trace (0) -> LogRecordSeverity.Trace (1)
            { 0, GetOpenTelemetrySeverityValue("Trace") },

            // NLog.LogLevel.Debug (1) -> LogRecordSeverity.Debug (5)
            { 1, GetOpenTelemetrySeverityValue("Debug") },

            // NLog.LogLevel.Info (2) -> LogRecordSeverity.Info (9)
            { 2, GetOpenTelemetrySeverityValue("Info") },

            // NLog.LogLevel.Warn (3) -> LogRecordSeverity.Warn (13)
            { 3, GetOpenTelemetrySeverityValue("Warn") },

            // NLog.LogLevel.Error (4) -> LogRecordSeverity.Error (17)
            { 4, GetOpenTelemetrySeverityValue("Error") },

            // NLog.LogLevel.Fatal (5) -> LogRecordSeverity.Fatal (21)
            { 5, GetOpenTelemetrySeverityValue("Fatal") }
        };

        return theoryData;
    }

    /// <summary>
    /// Tests that standard NLog log levels are correctly mapped to OpenTelemetry severity levels.
    /// This verifies that the bridge correctly translates NLog's ordinal-based level system
    /// to OpenTelemetry's severity enumeration.
    /// </summary>
    /// <param name="nlogLevelOrdinal">The NLog level ordinal value.</param>
    /// <param name="expectedOpenTelemetrySeverity">The expected OpenTelemetry severity level.</param>
    [Theory]
    [MemberData(nameof(GetLevelMappingData))]
    public void StandardNLogLevels_AreMappedCorrectly(int nlogLevelOrdinal, int expectedOpenTelemetrySeverity)
    {
        // Act
        var actualSeverity = OpenTelemetryNLogConverter.MapLogLevel(nlogLevelOrdinal);

        // Assert
        Assert.Equal(expectedOpenTelemetrySeverity, actualSeverity);
    }

    /// <summary>
    /// Tests that the NLog "Off" level (6) is handled correctly.
    /// The "Off" level should be mapped to Trace severity, though typically
    /// log events with "Off" level should be filtered out before reaching the target.
    /// </summary>
    [Fact]
    public void OffLevel_IsMappedToTrace()
    {
        // Arrange
        const int offLevelOrdinal = 6;
        var expectedSeverity = GetOpenTelemetrySeverityValue("Trace");

        // Act
        var actualSeverity = OpenTelemetryNLogConverter.MapLogLevel(offLevelOrdinal);

        // Assert
        Assert.Equal(expectedSeverity, actualSeverity);
    }

    /// <summary>
    /// Tests that unknown or invalid log level ordinals are mapped to Trace severity.
    /// This ensures the bridge is resilient to unexpected level values.
    /// </summary>
    /// <param name="invalidOrdinal">An invalid or unknown level ordinal.</param>
    [Theory]
    [InlineData(-1)]     // Negative ordinal
    [InlineData(7)]      // Beyond "Off"
    [InlineData(100)]    // Arbitrary high value
    [InlineData(int.MaxValue)] // Maximum integer value
    public void InvalidLevelOrdinals_AreMappedToTrace(int invalidOrdinal)
    {
        // Arrange
        var expectedSeverity = GetOpenTelemetrySeverityValue("Trace");

        // Act
        var actualSeverity = OpenTelemetryNLogConverter.MapLogLevel(invalidOrdinal);

        // Assert
        Assert.Equal(expectedSeverity, actualSeverity);
    }

    /// <summary>
    /// Tests that custom NLog levels between standard levels are mapped to the appropriate severity.
    /// This verifies that the range-based mapping logic works correctly for custom levels.
    /// </summary>
    /// <param name="nlogOrdinal">The NLog ordinal value.</param>
    /// <param name="expectedSeverity">The expected OpenTelemetry severity level.</param>
    [Theory]
    [InlineData(0, 1)]   // Trace (0) -> Should be Trace (1)
    [InlineData(1, 5)]   // Debug (1) -> Should be Debug (5)
    [InlineData(2, 9)]   // Info (2) -> Should be Info (9)
    [InlineData(3, 13)]  // Warn (3) -> Should be Warn (13)
    [InlineData(4, 17)]  // Error (4) -> Should be Error (17)
    public void CustomLevelsBetweenStandardLevels_AreMappedCorrectly(int nlogOrdinal, int expectedSeverity)
    {
        // Act
        var actualSeverity = OpenTelemetryNLogConverter.MapLogLevel(nlogOrdinal);

        // Assert
        Assert.Equal(expectedSeverity, actualSeverity);
    }

    /// <summary>
    /// Gets the numeric value of an OpenTelemetry log severity level by name.
    /// This helper method uses reflection to access the internal LogRecordSeverity enum
    /// since the Logs API is not yet public.
    /// </summary>
    /// <param name="severityName">The name of the severity level (e.g., "Info", "Error").</param>
    /// <returns>The numeric value of the severity level.</returns>
    private static int GetOpenTelemetrySeverityValue(string severityName)
    {
        return (int)Enum.Parse(OpenTelemetryLogSeverityType, severityName);
    }
}
