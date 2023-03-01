// <copyright file="OtelLoggingTests.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using FluentAssertions;
using OpenTelemetry.AutoInstrumentation.Logging;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests;

public class OtelLoggingTests
{
    [Fact]
    public void WhenNoFileSizeIsConfigured_Then_DefaultIsUsed()
    {
        var defaultSize = OtelLogging.GetConfiguredFileSizeLimitBytes();
        defaultSize.Should().Be(10 * 1024 * 1024);
    }

    [Fact]
    public void WhenValidFileSizeIsConfigured_Then_ItIsUsed()
    {
        var read = Environment.GetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_FILE_SIZE");
        try
        {
            Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_FILE_SIZE", "1024");
            OtelLogging.GetConfiguredFileSizeLimitBytes().Should().Be(1024);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_FILE_SIZE", read);
        }
    }

    [Fact]
    public void WhenInvalidFileSizeIsConfigured_Then_DefaultIsUsed()
    {
        var read = Environment.GetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_FILE_SIZE");
        try
        {
            Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_FILE_SIZE", "-1");
            OtelLogging.GetConfiguredFileSizeLimitBytes().Should().Be(10 * 1024 * 1024);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_FILE_SIZE", read);
        }
    }

    [Fact]
    public void WhenLogLevelIsNotConfigured_Then_DefaultIsUsed()
    {
        var read = Environment.GetEnvironmentVariable("OTEL_LOG_LEVEL");
        try
        {
            Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", string.Empty);

            OtelLogging.GetConfiguredLogLevel().Should().Be(LogLevel.Information);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", read);
        }
    }

    [Fact]
    public void WhenInvalidLogLevelIsConfigured_Then_DefaultIsUsed()
    {
        var read = Environment.GetEnvironmentVariable("OTEL_LOG_LEVEL");
        try
        {
            Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", "invalid");

            OtelLogging.GetConfiguredLogLevel().Should().Be(LogLevel.Information);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", read);
        }
    }

    [Fact]
    public void WhenValidLogLevelIsConfigured_Then_ItIsUsed()
    {
        var read = Environment.GetEnvironmentVariable("OTEL_LOG_LEVEL");
        try
        {
            Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", "warn");

            OtelLogging.GetConfiguredLogLevel().Should().Be(LogLevel.Warning);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", read);
        }
    }

    [Fact]
    public void WhenNoLoggingIsConfigured_Then_LogLevelHasNoValue()
    {
        var read = Environment.GetEnvironmentVariable("OTEL_LOG_LEVEL");
        try
        {
            Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", "none");

            OtelLogging.GetConfiguredLogLevel().Should().BeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", read);
        }
    }
}
