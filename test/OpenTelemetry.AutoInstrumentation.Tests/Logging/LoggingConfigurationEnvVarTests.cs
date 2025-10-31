// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Logging;

[Collection("Non-Parallel Collection")]
public class LoggingConfigurationEnvVarTests
{
    [Fact]
    public void WhenNoFileSizeIsConfigured_Then_DefaultIsUsed()
    {
        var cfg = new LoggingConfiguration();
        Assert.Equal(10 * 1024 * 1024, cfg.LogFileSize);
    }

    [Fact]
    public void WhenValidFileSizeIsConfigured_Then_ItIsUsed()
    {
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_FILE_SIZE", "1024");

        var cfg = new LoggingConfiguration();
        Assert.Equal(1024, cfg.LogFileSize);
    }

    [Fact]
    public void WhenInvalidFileSizeIsConfigured_Then_DefaultIsUsed()
    {
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_FILE_SIZE", "-1");

        var cfg = new LoggingConfiguration();
        Assert.Equal(10 * 1024 * 1024, cfg.LogFileSize);
    }

    [Fact]
    public void WhenLogLevelIsNotConfigured_Then_DefaultIsUsed()
    {
        Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", null);

        var cfg = new LoggingConfiguration();
        Assert.Equal(LogLevel.Information, cfg.LogLevel);
    }

    [Fact]
    public void WhenInvalidLogLevelIsConfigured_Then_DefaultIsUsed()
    {
        Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", "invalid");

        var cfg = new LoggingConfiguration();
        Assert.Equal(LogLevel.Information, cfg.LogLevel);
    }

    [Fact]
    public void WhenValidLogLevelIsConfigured_Then_ItIsUsed()
    {
        Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", "warn");

        var cfg = new LoggingConfiguration();
        Assert.Equal(LogLevel.Warning, cfg.LogLevel);
    }

    [Fact]
    public void WhenNoLoggingIsConfigured_Then_LogLevelHasNoValue()
    {
        Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", "none");

        var cfg = new LoggingConfiguration();
        Assert.Null(cfg.LogLevel);
    }

    [Fact]
    public void WhenLogSinkIsNotConfigured_Then_DefaultIsUsed()
    {
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGGER", null);

        var cfg = new LoggingConfiguration();
        Assert.Equal(LogSink.File, cfg.Logger);
    }

    [Fact]
    public void WhenInvalidLogSinkIsConfigured_Then_DefaultIsUsed()
    {
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGGER", "invalid");

        var cfg = new LoggingConfiguration();
        Assert.Equal(LogSink.File, cfg.Logger);
    }

    [Fact]
    public void WhenValidLogSinkIsConfigured_Then_ItIsUsed()
    {
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGGER", "console");

        var cfg = new LoggingConfiguration();
        Assert.Equal(LogSink.Console, cfg.Logger);
    }

    [Fact]
    public void WhenNoLogSinkIsConfigured_Then_NoOpSinkIsUsed()
    {
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGGER", "none");

        var cfg = new LoggingConfiguration();
        Assert.Equal(LogSink.NoOp, cfg.Logger);
    }
}
