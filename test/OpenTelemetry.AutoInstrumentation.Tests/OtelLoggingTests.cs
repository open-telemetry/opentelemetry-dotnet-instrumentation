// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using FluentAssertions;
using OpenTelemetry.AutoInstrumentation.Logging;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests;

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

    private static void UnsetLoggingEnvVars()
    {
        Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", null);
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_FILE_SIZE", null);
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGGER", null);
    }
}
