// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Tests;

public class InternalLoggerTests
{
    [Fact]
    public void WhenLogLevelIsConfigured_Then_OnlyEntriesWithEqualOrLowerLevelAreForwardedToSink()
    {
        var sink = new TestSink();
        var logger = new InternalLogger(sink, LogLevel.Information);

        // should be logged as it matches configured log level
        logger.Information("info message", false);
        // should be ignored, because LogLevel.Debug > LogLevel.Information
        logger.Debug("debug message", false);

        var messages = Assert.Single(sink.Messages);
        Assert.Contains("[Information] info message", messages, StringComparison.Ordinal);
    }

    [Fact]
    public void WhenEntryPassesLevelCheck_Then_EmitReceivesLevelRawMessageTimestampAndFlag()
    {
        var logger = new EmitRecordingLogger(LogLevel.Information);

        logger.Warning("w", true);

        var (level, rawMessage, timestampUtc, writeToEventLog) = Assert.Single(logger.Emitted);

        Assert.Equal(LogLevel.Warning, level);
        Assert.Equal("w", rawMessage);
        Assert.Equal(DateTimeKind.Utc, timestampUtc.Kind);
        Assert.True(writeToEventLog);
    }

    private sealed class EmitRecordingLogger(LogLevel level) : InternalLogger(NoopSink.Instance, level)
    {
        public List<(LogLevel Level, string RawMessage, DateTime TimestampUtc, bool WriteToEventLog)> Emitted { get; } = [];

        protected override void Emit(LogLevel level, string rawMessage, DateTime timestampUtc, bool writeToEventLog)
        {
            Emitted.Add((level, rawMessage, timestampUtc, writeToEventLog));
        }
    }
}
