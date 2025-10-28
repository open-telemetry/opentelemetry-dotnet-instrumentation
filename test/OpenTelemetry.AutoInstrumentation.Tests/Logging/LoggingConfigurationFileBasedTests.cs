// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Logging;

[Collection("Non-Parallel Collection")]
public class LoggingConfigurationFileBasedTests
{
    [Fact]
    public void FileBased_FixedValues_YamlApplied()
    {
        EnableFileBased("Logging/Files/TestLoggingFile.yaml");

        var cfg = new LoggingConfiguration();

        Assert.Equal(LogLevel.Debug, cfg.LogLevel);
        Assert.Equal(LogSink.Console, cfg.Logger);
        Assert.Equal(2048, cfg.LogFileSize);
        Assert.Equal("/var/log/opentelemetry/dotnet/app1", cfg.LogDirectory);
    }

    [Fact]
    public void FileBased_EnvInterpolation_YamlApplied()
    {
        Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", "warn");
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGGER", "file");
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_FILE_SIZE", "4096");
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_DIRECTORY", "/var/log/opentelemetry/dotnet/app1");

        EnableFileBased("Logging/Files/TestLoggingFileEnvVars.yaml");

        var cfg = new LoggingConfiguration();

        Assert.Equal(LogLevel.Warning, cfg.LogLevel);
        Assert.Equal(LogSink.File, cfg.Logger);
        Assert.Equal(4096, cfg.LogFileSize);
        Assert.Equal("/var/log/opentelemetry/dotnet/app1", cfg.LogDirectory);
    }

    private static void EnableFileBased(string yamlPath)
    {
        Environment.SetEnvironmentVariable("OTEL_EXPERIMENTAL_FILE_BASED_CONFIGURATION_ENABLED", "true");
        Environment.SetEnvironmentVariable("OTEL_EXPERIMENTAL_CONFIG_FILE", yamlPath);
    }
}
