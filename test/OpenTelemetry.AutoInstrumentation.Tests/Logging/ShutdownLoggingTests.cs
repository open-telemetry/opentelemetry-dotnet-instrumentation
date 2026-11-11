// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Tests.Util;
using OpenTelemetry.SelfDiagnostics;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace OpenTelemetry.AutoInstrumentation.Tests.Logging;

[Collection("Non-Parallel Collection")]
public sealed class ShutdownLoggingTests
{
    [Theory]
    [InlineData(null, "file", true)]
    [InlineData("error", "console", true)]
    [InlineData("none", "file", false)]
    [InlineData(" NONE ", "file", false)]
    [InlineData(null, "none", false)]
    public void WhenSdkNeverStarted_Then_ShutdownErrorRespectsLoggingSettings(string? level, string sink, bool expectedOutput) =>
        AssertShutdownOutput(level, sink, null, expectedOutput);

    [Theory]
    [InlineData(LogLevel.Error, "logs", false, false, true)]
    [InlineData(LogLevel.Error, null, true, false, true)]
    [InlineData(LogLevel.Error, null, false, true, true)]
    [InlineData(LogLevel.None, "logs", false, false, false)]
    [InlineData(LogLevel.Error, null, false, false, false)]
    public void WhenSdkListenerIsUnavailable_Then_ShutdownErrorRespectsResolvedOptions(LogLevel level, string? directory, bool stdout, bool stderr, bool expectedOutput)
    {
        var options = new SelfDiagnosticsOptions
        {
            MinimumLevel = level,
            LogDirectory = directory,
            LogToStdout = stdout,
            LogToStderr = stderr,
        };

        // Resolved SDK options take precedence over the automatic instrumentation's sink setting.
        AssertShutdownOutput(null, "none", options, expectedOutput);
    }

    [Theory]
    [InlineData("none")]
    [InlineData(" NONE ")]
    public void WhenResolvedOptionsEnableLogging_Then_EnvironmentNoneDoesNotSuppressShutdownErrors(string initialLevel)
    {
        var options = new SelfDiagnosticsOptions
        {
            MinimumLevel = LogLevel.Error,
            LogToStderr = true,
        };

        AssertShutdownOutput(initialLevel, "none", options, true);
    }

    private static void AssertShutdownOutput(string? level, string sink, SelfDiagnosticsOptions? options, bool expectedOutput)
    {
        var scope = new EnvironmentScope(new Dictionary<string, string?>
        {
            { "OTEL_LOG_LEVEL", level },
            { "OTEL_DOTNET_AUTO_LOGGER", sink },
        });

        var originalWriter = Console.Error;
        using var writer = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            OtelLogging.Reset();
            if (options != null)
            {
                OtelLogging.SetSelfDiagnosticsOptions(options);
            }

            Console.SetError(writer);
            var exception = new InvalidOperationException("shutdown failure");

            OtelLogging.WriteShutdownError(exception);

            if (expectedOutput)
            {
                Assert.Contains("OpenTelemetry Automatic Instrumentation shutdown failed:", writer.ToString(), StringComparison.Ordinal);
                Assert.Contains(exception.ToString(), writer.ToString(), StringComparison.Ordinal);
            }
            else
            {
                Assert.Empty(writer.ToString());
            }
        }
        finally
        {
            Console.SetError(originalWriter);
            scope.Dispose();
            OtelLogging.Reset();
        }
    }
}
