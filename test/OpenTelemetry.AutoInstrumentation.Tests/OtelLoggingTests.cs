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
        UnsetLogLevelEnvVar();
    }

    public void Dispose()
    {
        UnsetLogLevelEnvVar();
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

    private static void UnsetLogLevelEnvVar()
    {
        Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", null);
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_FILE_SIZE", null);
    }
}
