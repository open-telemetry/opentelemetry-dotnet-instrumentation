// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Tests.Util;

namespace OpenTelemetry.AutoInstrumentation.Tests;

[Collection("Non-Parallel Collection")]
public sealed class OtelLoggingTests
{
    [Fact]
    public void WhenNoFileSizeIsConfigured_Then_DefaultIsUsed()
    {
        using var scope = new EnvironmentScope(new Dictionary<string, string?>
        {
            { "OTEL_DOTNET_AUTO_LOG_FILE_SIZE", null },
        });

        var defaultSize = OtelLogging.GetConfiguredFileSizeLimitBytes();
        Assert.Equal(10 * 1024 * 1024, defaultSize);
    }

    [Fact]
    public void WhenValidFileSizeIsConfigured_Then_ItIsUsed()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { "OTEL_DOTNET_AUTO_LOG_FILE_SIZE", "1024" }
        });

        Assert.Equal(1024, OtelLogging.GetConfiguredFileSizeLimitBytes());
    }

    [Fact]
    public void WhenInvalidFileSizeIsConfigured_Then_DefaultIsUsed()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { "OTEL_DOTNET_AUTO_LOG_FILE_SIZE", "-1" }
        });

        Assert.Equal(10 * 1024 * 1024, OtelLogging.GetConfiguredFileSizeLimitBytes());
    }

    [Fact]
    public void WhenLogLevelIsNotConfigured_Then_DefaultIsUsed()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { "OTEL_LOG_LEVEL", null }
        });

        Assert.Equal(LogLevel.Information, OtelLogging.GetConfiguredLogLevel());
    }

    [Fact]
    public void WhenInvalidLogLevelIsConfigured_Then_DefaultIsUsed()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { "OTEL_LOG_LEVEL", "invalid" }
        });

        Assert.Equal(LogLevel.Information, OtelLogging.GetConfiguredLogLevel());
    }

    [Fact]
    public void WhenValidLogLevelIsConfigured_Then_ItIsUsed()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { "OTEL_LOG_LEVEL", "warn" }
        });

        Assert.Equal(LogLevel.Warning, OtelLogging.GetConfiguredLogLevel());
    }

    [Fact]
    public void WhenNoLoggingIsConfigured_Then_LogLevelHasNoValue()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { "OTEL_LOG_LEVEL", "none" }
        });

        Assert.Null(OtelLogging.GetConfiguredLogLevel());
    }

    [Fact]
    public void WhenLogSinkIsNotConfigured_Then_DefaultIsUsed()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { "OTEL_DOTNET_AUTO_LOGGER", null }
        });

        Assert.Equal(LogSink.File, OtelLogging.GetConfiguredLogSink());
    }

    [Fact]
    public void WhenInvalidLogSinkIsConfigured_Then_DefaultIsUsed()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { "OTEL_DOTNET_AUTO_LOGGER", "invalid" }
        });

        Assert.Equal(LogSink.File, OtelLogging.GetConfiguredLogSink());
    }

    [Fact]
    public void WhenValidLogSinkIsConfigured_Then_ItIsUsed()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { "OTEL_DOTNET_AUTO_LOGGER", "console" }
        });

        Assert.Equal(LogSink.Console, OtelLogging.GetConfiguredLogSink());
    }

    [Fact]
    public void WhenNoLogSinkIsConfigured_Then_NoOpSinkIsUsed()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { "OTEL_DOTNET_AUTO_LOGGER", "none" }
        });

        Assert.Equal(LogSink.NoOp, OtelLogging.GetConfiguredLogSink());
    }

    [Fact]
    public void WhenFileSinkIsUsed_Then_FileContentIsDetected()
    {
        var tempLogsDirectory = DirectoryHelpers.CreateTempDirectory();

        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { "OTEL_LOG_LEVEL", "debug" },
            { "OTEL_DOTNET_AUTO_LOGGER", "file" },
            { "OTEL_DOTNET_AUTO_LOG_DIRECTORY", tempLogsDirectory.FullName },
        });

        try
        {
            // Reset internal state
            OtelLogging.Reset();

            var logger = OtelLogging.GetLogger("FileUnitTests");
            var logLine = "== Test Log Here ==";

            logger.Debug(logLine, false);
            logger.Close(); // Shutdown the logger to release the file

            var files = tempLogsDirectory.GetFiles();

            var file = Assert.Single(files);

            var content = File.ReadAllText(file.FullName);

            Assert.Contains(logLine, content, StringComparison.Ordinal);
        }
        finally
        {
            tempLogsDirectory.Delete(true);
        }
    }

    [Fact]
    public void WhenConsoleSinkIsUsed_Then_ConsoleContentIsDetected()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { "OTEL_LOG_LEVEL", "debug" },
            { "OTEL_DOTNET_AUTO_LOGGER", "console" },
        });

        var currentWriter = Console.Out;

        using var ms = new MemoryStream();
        using var tw = new StreamWriter(ms);

        Console.SetOut(tw);

        try
        {
            // Reset internal state
            OtelLogging.Reset();

            var logger = OtelLogging.GetLogger("ConsoleUnitTests");
            var logLine = "== Test Log Here ==";

            logger.Debug(logLine, false);
            tw.Flush(); // Forces rows to be written

            using TextReader reader = new StreamReader(ms);

            ms.Position = 0; // reset reading position
            var content = reader.ReadToEnd();

            Assert.Contains(logLine, content, StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(currentWriter);
        }
    }

    [Fact]
    public void AfterLoggerIsClosed_ConsecutiveLogCallsWithTheSameLoggerAreNotWrittenToConfiguredSink()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { "OTEL_LOG_LEVEL", "debug" },
            { "OTEL_DOTNET_AUTO_LOGGER", "console" },
        });

        var currentWriter = Console.Out;

        using var ms = new MemoryStream();
        using var tw = new StreamWriter(ms);

        Console.SetOut(tw);

        try
        {
            // Reset internal state
            OtelLogging.Reset();

            var loggerSuffix = "ConsoleUnitTests";
            var logger = OtelLogging.GetLogger(loggerSuffix);

            var expectedLogContent = "== Test Log Here ==";
            LogAndFlush(logger, expectedLogContent, tw);

            using var streamReader = new StreamReader(ms);

            var content = ReadWrittenContent(ms, streamReader);
            Assert.Contains(expectedLogContent, content, StringComparison.Ordinal);

            // Reset
            ms.SetLength(0);

            OtelLogging.CloseLogger(loggerSuffix, logger);

            LogAndFlush(logger, expectedLogContent, tw);

            content = ReadWrittenContent(ms, streamReader);
            Assert.Empty(content);
        }
        finally
        {
            Console.SetOut(currentWriter);
        }
    }

    [Fact]
    public void AfterLoggerIsClosed_ConsecutiveCallsToGetLoggerReturnNoopLogger()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { "OTEL_LOG_LEVEL", "debug" },
            { "OTEL_DOTNET_AUTO_LOGGER", "console" },
        });

        var currentWriter = Console.Out;

        using var ms = new MemoryStream();
        using var tw = new StreamWriter(ms);

        Console.SetOut(tw);

        try
        {
            // Reset internal state
            OtelLogging.Reset();

            var loggerSuffix = "ConsoleUnitTests";
            var logger = OtelLogging.GetLogger(loggerSuffix);

            var expectedLogContent = "== Test Log Here ==";
            LogAndFlush(logger, expectedLogContent, tw);

            using var streamReader = new StreamReader(ms);

            var content = ReadWrittenContent(ms, streamReader);
            Assert.Contains(expectedLogContent, content, StringComparison.Ordinal);

            // Reset
            ms.SetLength(0);

            OtelLogging.CloseLogger(loggerSuffix, logger);

            logger = OtelLogging.GetLogger(loggerSuffix);
            LogAndFlush(logger, expectedLogContent, tw);

            content = ReadWrittenContent(ms, streamReader);
            Assert.Empty(content);
        }
        finally
        {
            Console.SetOut(currentWriter);
        }
    }

    [Fact]
    public void WhenManagedLoggerIsRequested_Then_ItIsRoutedThroughSdkSelfDiagnostics()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { "OTEL_LOG_LEVEL", null },
        });

        OtelLogging.Reset();

        Assert.IsType<SelfDiagnosticsLogger>(OtelLogging.GetLogger());
    }

    [Fact]
    public void WhenSuffixIsNeitherManagedNorOpAmp_Then_GetLoggerReturnsInternalLogger()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { "OTEL_LOG_LEVEL", null },
            { "OTEL_DOTNET_AUTO_LOGGER", "none" },
        });

        OtelLogging.Reset();

        var logger = OtelLogging.GetLogger("FileUnitTests");

        Assert.IsType<InternalLogger>(logger);
        Assert.IsNotType<SelfDiagnosticsLogger>(logger);
    }

    [Fact]
    public void WhenRoutingDecisionWasMade_Then_LoggersCreatedLaterInheritIt()
    {
        using var scope = new EnvironmentScope(new Dictionary<string, string?>
        {
            { "OTEL_LOG_LEVEL", null },
            { "OTEL_DOTNET_AUTO_LOGGER", "none" },
        });

        OtelLogging.Reset();
        try
        {
            var early = Assert.IsType<SelfDiagnosticsLogger>(OtelLogging.GetLogger());
            Assert.Equal(SelfDiagnosticsLogger.RoutingMode.Buffering, early.CurrentRoutingMode);

            OtelLogging.UseFallbackIfStillBuffering();

            var late = Assert.IsType<SelfDiagnosticsLogger>(OtelLogging.GetLogger("OpAmp"));
            Assert.Equal(SelfDiagnosticsLogger.RoutingMode.Fallback, early.CurrentRoutingMode);
            Assert.Equal(SelfDiagnosticsLogger.RoutingMode.Fallback, late.CurrentRoutingMode);
        }
        finally
        {
            OtelLogging.Reset();
        }
    }

    [Theory]
    [InlineData("trace", (int)LogLevel.Debug)]
    [InlineData("DEBUG", (int)LogLevel.Debug)]
    [InlineData(" debug ", (int)LogLevel.Debug)]
    [InlineData("warning", (int)LogLevel.Warning)]
    [InlineData("information", (int)LogLevel.Information)]
    [InlineData("invalid", (int)LogLevel.Warning)]
    [InlineData(" NONE ", null)]
    public void WhenManagedLoggerIsCreated_Then_BufferFilteringMatchesSdkLevelParsing(string value, int? expectedLevel)
    {
        using var scope = new EnvironmentScope(new Dictionary<string, string?>
        {
            { "OTEL_LOG_LEVEL", value },
            { "OTEL_DOTNET_AUTO_LOGGER", "none" },
        });

        OtelLogging.Reset();
        try
        {
            var logger = OtelLogging.GetLogger();
            if (expectedLevel.HasValue)
            {
                var level = (LogLevel)expectedLevel.Value;
                Assert.Equal(level, logger.Level);
                Assert.Equal(level == LogLevel.Debug, logger.IsEnabled(LogLevel.Debug));
            }
            else
            {
                Assert.False(logger.IsEnabled(LogLevel.Error), "Expected logger to be disabled for Error level when OTEL_LOG_LEVEL is none.");
            }
        }
        finally
        {
            OtelLogging.Reset();
        }
    }

    [Fact]
    public void WhenManagedLoggingIsDisabled_Then_FallbackDoesNotCreateFiles()
    {
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        using var scope = new EnvironmentScope(new Dictionary<string, string?>
        {
            { "OTEL_LOG_LEVEL", "none" },
            { "OTEL_DOTNET_AUTO_LOGGER", "file" },
            { "OTEL_DOTNET_AUTO_LOG_DIRECTORY", directory },
        });

        OtelLogging.Reset();
        try
        {
            var logger = OtelLogging.GetLogger();
            logger.Error("before fallback", true);
            OtelLogging.UseFallbackIfStillBuffering();
            logger.Error("after fallback", true);
            OtelLogging.GetLogger("OpAmp").Error("late logger", true);

            Assert.False(Directory.Exists(directory));
        }
        finally
        {
            OtelLogging.Reset();
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    private static void LogAndFlush(IOtelLogger logger, string logLine, StreamWriter? tw)
    {
        logger.Debug(logLine, false);
        tw?.Flush(); // Forces rows to be written
    }

    private static string ReadWrittenContent(MemoryStream ms, StreamReader streamReader)
    {
        ms.Position = 0; // reset reading position
        return streamReader.ReadToEnd();
    }
}
