// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using FluentAssertions;
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
        defaultSize.Should().Be(10 * 1024 * 1024);
    }

    [Fact]
    public void WhenValidFileSizeIsConfigured_Then_ItIsUsed()
    {
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_FILE_SIZE", "1024");

        OtelLogging.GetConfiguredFileSizeLimitBytes().Should().Be(1024);
    }

    [Fact]
    public void WhenInvalidFileSizeIsConfigured_Then_DefaultIsUsed()
    {
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_FILE_SIZE", "-1");

        OtelLogging.GetConfiguredFileSizeLimitBytes().Should().Be(10 * 1024 * 1024);
    }

    [Fact]
    public void WhenLogLevelIsNotConfigured_Then_DefaultIsUsed()
    {
        Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", null);

        OtelLogging.GetConfiguredLogLevel().Should().Be(LogLevel.Information);
    }

    [Fact]
    public void WhenInvalidLogLevelIsConfigured_Then_DefaultIsUsed()
    {
        Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", "invalid");

        OtelLogging.GetConfiguredLogLevel().Should().Be(LogLevel.Information);
    }

    [Fact]
    public void WhenValidLogLevelIsConfigured_Then_ItIsUsed()
    {
        Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", "warn");

        OtelLogging.GetConfiguredLogLevel().Should().Be(LogLevel.Warning);
    }

    [Fact]
    public void WhenNoLoggingIsConfigured_Then_LogLevelHasNoValue()
    {
        Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", "none");

        OtelLogging.GetConfiguredLogLevel().Should().BeNull();
    }

    [Fact]
    public void WhenLogSinkIsNotConfigured_Then_DefaultIsUsed()
    {
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGGER", null);

        OtelLogging.GetConfiguredLogSink().Should().Be(LogSink.File);
    }

    [Fact]
    public void WhenInvalidLogSinkIsConfigured_Then_DefaultIsUsed()
    {
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGGER", "invalid");

        OtelLogging.GetConfiguredLogSink().Should().Be(LogSink.File);
    }

    [Fact]
    public void WhenValidLogSinkIsConfigured_Then_ItIsUsed()
    {
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGGER", "console");

        OtelLogging.GetConfiguredLogSink().Should().Be(LogSink.Console);
    }

    [Fact]
    public void WhenNoLogSinkIsConfigured_Then_NoOpSinkIsUsed()
    {
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGGER", "none");

        OtelLogging.GetConfiguredLogSink().Should().Be(LogSink.NoOp);
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

            files.Length.Should().Be(1);

            var content = File.ReadAllText(files[0].FullName);

            content.Should().Contain(logLine);
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

            content.Should().Contain(logLine);
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
