// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Tests.Util;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests;

[Collection("Non-Parallel Collection")]
public class OtelLoggingTests : IDisposable
{
    public OtelLoggingTests()
    {
        UnsetLoggingEnvVars();
    }

    public void Dispose()
    {
        UnsetLoggingEnvVars();
    }

    [Fact]
    public void WhenNoFileSizeIsConfigured_Then_DefaultIsUsed()
    {
        var defaultSize = OtelLogging.GetConfiguredFileSizeLimitBytes();
        Assert.Equal(10 * 1024 * 1024, defaultSize);
    }

    [Fact]
    public void WhenValidFileSizeIsConfigured_Then_ItIsUsed()
    {
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_FILE_SIZE", "1024");

        Assert.Equal(1024, OtelLogging.GetConfiguredFileSizeLimitBytes());
    }

    [Fact]
    public void WhenInvalidFileSizeIsConfigured_Then_DefaultIsUsed()
    {
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_FILE_SIZE", "-1");

        Assert.Equal(10 * 1024 * 1024, OtelLogging.GetConfiguredFileSizeLimitBytes());
    }

    [Fact]
    public void WhenLogLevelIsNotConfigured_Then_DefaultIsUsed()
    {
        Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", null);

        Assert.Equal(LogLevel.Information, OtelLogging.GetConfiguredLogLevel());
    }

    [Fact]
    public void WhenInvalidLogLevelIsConfigured_Then_DefaultIsUsed()
    {
        Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", "invalid");

        Assert.Equal(LogLevel.Information, OtelLogging.GetConfiguredLogLevel());
    }

    [Fact]
    public void WhenValidLogLevelIsConfigured_Then_ItIsUsed()
    {
        Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", "warn");

        Assert.Equal(LogLevel.Warning, OtelLogging.GetConfiguredLogLevel());
    }

    [Fact]
    public void WhenNoLoggingIsConfigured_Then_LogLevelHasNoValue()
    {
        Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", "none");

        Assert.Null(OtelLogging.GetConfiguredLogLevel());
    }

    [Fact]
    public void WhenLogSinkIsNotConfigured_Then_DefaultIsUsed()
    {
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGGER", null);

        Assert.Equal(LogSink.File, OtelLogging.GetConfiguredLogSink());
    }

    [Fact]
    public void WhenInvalidLogSinkIsConfigured_Then_DefaultIsUsed()
    {
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGGER", "invalid");

        Assert.Equal(LogSink.File, OtelLogging.GetConfiguredLogSink());
    }

    [Fact]
    public void WhenValidLogSinkIsConfigured_Then_ItIsUsed()
    {
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGGER", "console");

        Assert.Equal(LogSink.Console, OtelLogging.GetConfiguredLogSink());
    }

    [Fact]
    public void WhenNoLogSinkIsConfigured_Then_NoOpSinkIsUsed()
    {
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGGER", "none");

        Assert.Equal(LogSink.NoOp, OtelLogging.GetConfiguredLogSink());
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
            logger.Dispose(); // Dispose the logger to release the file

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

        var currentWritter = Console.Out;

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

            Assert.Contains(logLine, content);
        }
        finally
        {
            Console.SetOut(currentWritter);
        }
    }

    private static void UnsetLoggingEnvVars()
    {
        Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", null);
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_FILE_SIZE", null);
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGGER", null);
    }
}
