// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Tests.Util;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Logging;

[Collection("Non-Parallel Collection")]
public class OtelLoggingTests : IDisposable
{
    public OtelLoggingTests()
    {
        UnsetAllLoggingEnvVars();
    }

    public void Dispose()
    {
        UnsetAllLoggingEnvVars();
    }

    [Fact]
    public void WhenFileSinkIsUsed_Then_FileContentIsDetected()
    {
        Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", "debug");
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGGER", "file");

        var tempLogsDirectory = DirectoryHelpers.CreateTempDirectory();
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_DIRECTORY", tempLogsDirectory.FullName);

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
            Assert.Contains(logLine, content);
        }
        finally
        {
            tempLogsDirectory.Delete(true);
        }
    }

    [Fact]
    public void WhenConsoleSinkIsUsed_Then_ConsoleContentIsDetected()
    {
        Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", "debug");
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGGER", "console");

        var currentWriter = Console.Out;

        using var ms = new MemoryStream();
        using var tw = new StreamWriter(ms);
        Console.SetOut(tw);

        try
        {
            OtelLogging.Reset();

            var logger = OtelLogging.GetLogger("ConsoleUnitTests");
            var logLine = "== Test Log Here ==";

            logger.Debug(logLine, false);
            tw.Flush(); // Forces rows to be written

            using TextReader reader = new StreamReader(ms);

            ms.Position = 0; // reset reading position
            var content = reader.ReadToEnd();

            Assert.Contains(logLine, content);
        }
        finally
        {
            Console.SetOut(currentWriter);
        }
    }

    [Fact]
    public void AfterLoggerIsClosed_ConsecutiveLogCallsWithTheSameLoggerAreNotWrittenToConfiguredSink()
    {
        Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", "debug");
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGGER", "console");

        var currentWriter = Console.Out;

        using var ms = new MemoryStream();
        using var tw = new StreamWriter(ms);
        Console.SetOut(tw);

        try
        {
            OtelLogging.Reset();

            var loggerSuffix = "ConsoleUnitTests";
            var logger = OtelLogging.GetLogger(loggerSuffix);

            var expectedLogContent = "== Test Log Here ==";
            LogAndFlush(logger, expectedLogContent, tw);

            using var streamReader = new StreamReader(ms);

            var content = ReadWrittenContent(ms, streamReader);
            Assert.Contains(expectedLogContent, content);

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
        Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", "debug");
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGGER", "console");

        var currentWriter = Console.Out;

        using var ms = new MemoryStream();
        using var tw = new StreamWriter(ms);
        Console.SetOut(tw);

        try
        {
            OtelLogging.Reset();

            var loggerSuffix = "ConsoleUnitTests";
            var logger = OtelLogging.GetLogger(loggerSuffix);

            var expectedLogContent = "== Test Log Here ==";
            LogAndFlush(logger, expectedLogContent, tw);

            using var streamReader = new StreamReader(ms);

            var content = ReadWrittenContent(ms, streamReader);
            Assert.Contains(expectedLogContent, content);

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

    private static void LogAndFlush(IOtelLogger logger, string line, StreamWriter? tw)
    {
        logger.Debug(line, false);
        tw?.Flush();
    }

    private static string ReadWrittenContent(MemoryStream ms, StreamReader reader)
    {
        ms.Position = 0;
        return reader.ReadToEnd();
    }

    private static void UnsetAllLoggingEnvVars()
    {
        // logging
        Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", null);
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_FILE_SIZE", null);
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGGER", null);
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_DIRECTORY", null);

        // force ENV mode in tests (disable YAML)
        Environment.SetEnvironmentVariable("OTEL_EXPERIMENTAL_FILE_BASED_CONFIGURATION_ENABLED", "false");
        Environment.SetEnvironmentVariable("OTEL_EXPERIMENTAL_CONFIG_FILE", null);
    }
}
