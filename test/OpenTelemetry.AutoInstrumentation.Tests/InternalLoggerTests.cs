// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;
using Xunit;

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
}
