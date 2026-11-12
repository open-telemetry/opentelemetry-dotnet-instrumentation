// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using Google.Protobuf;
using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;

namespace IntegrationTests;

public class SerilogBridgeTests : TestHelper
{
    public SerilogBridgeTests(ITestOutputHelper output)
        : base("SerilogBridge", output)
    {
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [MemberData(nameof(LibraryVersion.Serilog), MemberType = typeof(LibraryVersion))]
    public void SubmitLogs_ThroughSerilogBridge_WhenSerilogIsUsedDirectlyForLogging(string packageVersion)
    {
        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);

        collector.ExpectCollected(
            records =>
                records.Count == 5 &&
                records.Count(logRecord =>
                    VerifyBody(logRecord, "Hello {Name}, count {Count}, flag {Flag}, ratio {Ratio}, at {When}") &&
                    logRecord is { SeverityText: "Information", SeverityNumber: SeverityNumber.Info } &&
                    VerifyTraceContext(logRecord) &&
                    VerifyPropertyAttributes(logRecord) &&
                    collector.GetScopeName(logRecord) == "TestApplication.SerilogBridge.Program") == 1 &&
                records.Count(logRecord =>
                    VerifyBody(logRecord, "Structured {@Point} and {Items}") &&
                    VerifyStructuredAttributes(logRecord)) == 1 &&
                records.Count(logRecord =>
                    VerifyBody(logRecord, "Nested {@Points} and {@Levels}") &&
                    VerifyNestedStructuredAttributes(logRecord)) == 1 &&
                records.Count(logRecord =>
                    VerifyBody(logRecord, "Logged outside of an activity") &&
                    logRecord is { SeverityText: "Warning", SeverityNumber: SeverityNumber.Warn } &&
                    logRecord.TraceId == ByteString.Empty &&
                    logRecord.SpanId == ByteString.Empty) == 1 &&
                records.Count(logRecord =>
                    VerifyBody(logRecord, "Exception occurred") &&
                    logRecord is { SeverityText: "Error", SeverityNumber: SeverityNumber.Error } &&
                    VerifyExceptionAttributes(logRecord) &&
                    logRecord.Attributes.Count == 3) == 1,
            "Expected the three Information records, the Warning record and the Error record, each exported once.");

        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_ENABLE_SERILOG_BRIDGE", "true");

        RunTestApplication(new()
        {
            PackageVersion = packageVersion,
            Arguments = "--mode direct"
        });

        collector.AssertCollected();
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [MemberData(nameof(LibraryVersion.Serilog), MemberType = typeof(LibraryVersion))]
    public void SubmitLogs_ThroughSerilogBridgeWithRenderedMessage_WhenFormattedMessageIsIncluded(string packageVersion)
    {
        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);

        // Serilog's default rendering quotes strings and formats with the invariant culture, also under de-DE.
        collector.ExpectCollected(
            records =>
                records.Count(logRecord =>
                    VerifyBody(logRecord, "Hello {Name}, count {Count}, flag {Flag}, ratio {Ratio}, at {When}") &&
                    VerifyStringAttribute(logRecord, "serilog.rendered_message", "Hello \"world\", count 42, flag True, ratio 1.5, at 01/02/2026 03:04:05")) == 1 &&
                records.Count(logRecord =>
                    VerifyBody(logRecord, "Logged outside of an activity") &&
                    VerifyStringAttribute(logRecord, "serilog.rendered_message", "Logged outside of an activity")) == 1,
            "Expected the rendered message on records with and without properties.");

        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_ENABLE_SERILOG_BRIDGE", "true");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE", "true");

        RunTestApplication(new()
        {
            PackageVersion = packageVersion,
            Arguments = "--mode direct"
        });

        collector.AssertCollected();
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [MemberData(nameof(LibraryVersion.Serilog), MemberType = typeof(LibraryVersion))]
    public void SubmitLogs_ThroughSerilogBridgeOnce_WhenSubLoggerIsConfigured(string packageVersion)
    {
        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);

        collector.ExpectCollected(
            records =>
                records.Count == 1 &&
                VerifyBody(records.Single(), "Logged through a root logger with a sub-logger") &&
                collector.GetScopeName(records.Single()) is { Length: 0 },
            "Expected the record to be exported once, not once more for the sub-logger.");

        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_ENABLE_SERILOG_BRIDGE", "true");

        RunTestApplication(new()
        {
            PackageVersion = packageVersion,
            Arguments = "--mode sub-logger"
        });

        collector.AssertCollected();
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [MemberData(nameof(LibraryVersion.Serilog), MemberType = typeof(LibraryVersion))]
    public void NoLogsAreSubmitted_ThroughSerilogBridge_WhenBridgeIsNotEnabled(string packageVersion)
    {
        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);

        EnableBytecodeInstrumentation();

        RunTestApplication(new()
        {
            PackageVersion = packageVersion,
            Arguments = "--mode direct"
        });

        collector.AssertEmpty();
    }

    [SkippableTheory]
    [Trait("Category", "EndToEnd")]
    [MemberData(nameof(LibraryVersion.Serilog), MemberType = typeof(LibraryVersion))]
    public void SubmitLogs_ThroughSerilogBridge_WhenSerilogReplacesILoggerFactory(string packageVersion)
    {
        SkipIfSerilogCompanionPackagesAreNotTested(packageVersion);

        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);

        collector.ExpectCollected(
            records =>
                records.Count(logRecord =>
                    VerifyBody(logRecord, "Logged through ILogger {Source}") &&
                    VerifyStringAttribute(logRecord, "Source", "hosting")) == 1 &&
                records.Count(logRecord =>
                    VerifyBody(logRecord, "Logged through Serilog {Source}") &&
                    VerifyStringAttribute(logRecord, "Source", "hosting")) == 1,
            "Expected the ILogger record and the Serilog record, each exported once.");

        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_ENABLE_SERILOG_BRIDGE", "true");

        RunTestApplication(new()
        {
            PackageVersion = packageVersion,
            Arguments = "--mode use-serilog"
        });

        collector.AssertCollected();
    }

#if NET
    // The Serilog bridge skips only the events that LoggerProviderCollectionSink already forwards to the OpenTelemetry ILogger provider.
    [SkippableTheory]
    [Trait("Category", "EndToEnd")]
    [MemberData(nameof(LibraryVersion.Serilog), MemberType = typeof(LibraryVersion))]
    public void SubmitLogs_WithoutDuplicates_WhenSerilogWritesToILoggerProviders(string packageVersion)
    {
        SkipIfSerilogCompanionPackagesAreNotTested(packageVersion);

        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);

        collector.ExpectCollected(
            records =>
                records.Count(logRecord => VerifyBody(logRecord, "Logged through ILogger {Source}")) == 1 &&
                records.Count(logRecord => VerifyBody(logRecord, "Logged through Serilog {Source}")) == 1 &&
                records.Count(logRecord => VerifyBody(logRecord, "Logged through Serilog before the host is built")) == 1 &&
                records.Count(logRecord => VerifyBody(logRecord, "Logged through Serilog to providers that the host does not use")) == 1 &&
                records.Count(logRecord => VerifyBody(logRecord, "Logged through Serilog to three provider collections")) == 1,
            "Expected each record exported once, also before the host is built, for providers that the host does not use and for three provider collections.");

        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_ENABLE_SERILOG_BRIDGE", "true");

        RunTestApplication(new()
        {
            PackageVersion = packageVersion,
            Arguments = "--mode static-logger-write-to-providers"
        });

        collector.AssertCollected();
    }
#endif

    // Without the ILogger bridge, no OpenTelemetry provider receives the events that LoggerProviderCollectionSink forwards,
    // unless the application adds its own. That provider exports its events itself, so the bridge skips them.
    [SkippableTheory]
    [Trait("Category", "EndToEnd")]
    [MemberData(nameof(LibraryVersion.Serilog), MemberType = typeof(LibraryVersion))]
    public void SubmitLogs_ThroughSerilogBridge_WhenSerilogWritesToILoggerProvidersWithoutILoggerBridge(string packageVersion)
    {
        SkipIfSerilogCompanionPackagesAreNotTested(packageVersion);

        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);

        collector.ExpectCollected(
            records =>
                records.Count(logRecord => VerifyBody(logRecord, "Logged through ILogger {Source}")) == 1 &&
                records.Count(logRecord => VerifyBody(logRecord, "Logged through Serilog {Source}")) == 1 &&
                !records.Any(logRecord => VerifyBody(logRecord, "Logged through Serilog to an application OpenTelemetry provider")),
            "Expected the ILogger record and the Serilog record, each exported once, and no record for the application OpenTelemetry provider.");

        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_ENABLE_SERILOG_BRIDGE", "true");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_ILOGGER_INSTRUMENTATION_ENABLED", "false");

        RunTestApplication(new()
        {
            PackageVersion = packageVersion,
            Arguments = "--mode static-logger-write-to-providers"
        });

        collector.AssertCollected();
    }

    [SkippableTheory]
    [Trait("Category", "EndToEnd")]
    [MemberData(nameof(LibraryVersion.Serilog), MemberType = typeof(LibraryVersion))]
    public void SubmitLogs_ThroughSerilogBridge_OnlyForLoggersWithoutApplicationOpenTelemetrySink(string packageVersion)
    {
        SkipIfSerilogCompanionPackagesAreNotTested(packageVersion);

        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);

        // The application sink exports the first two records itself, so the bridge must skip those loggers.
        collector.ExpectCollected(
            records =>
                records.Count(logRecord => VerifyBody(logRecord, "Logged through the application OpenTelemetry sink")) == 1 &&
                records.Count(logRecord => VerifyBody(logRecord, "Logged through the application OpenTelemetry sink in a sub-logger")) == 1 &&
                records.Count(logRecord => VerifyBody(logRecord, "Logged through a logger without the application OpenTelemetry sink")) == 1,
            "Expected each record exported once.");

        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_ENABLE_SERILOG_BRIDGE", "true");

        RunTestApplication(new()
        {
            PackageVersion = packageVersion,
            Arguments = "--mode opentelemetry-sink"
        });

        collector.AssertCollected();
    }

    private static void SkipIfSerilogCompanionPackagesAreNotTested(string packageVersion)
    {
        if (!string.IsNullOrEmpty(packageVersion) && Version.Parse(packageVersion) < new Version(3, 1, 1))
        {
            throw new SkipException("Serilog.Extensions.Hosting and Serilog.Sinks.OpenTelemetry are not tested with Serilog < 3.1.1.");
        }
    }

    private static bool VerifyPropertyAttributes(LogRecord logRecord)
    {
        // The application logs under de-DE, so a culture-sensitive conversion changes the DateTime value.
        return VerifyStringAttribute(logRecord, "Name", "world") &&
               logRecord.Attributes.SingleOrDefault(value => value.Key == "Count")?.Value is { ValueCase: AnyValue.ValueOneofCase.IntValue, IntValue: 42 } &&
               logRecord.Attributes.SingleOrDefault(value => value.Key == "Flag")?.Value is { ValueCase: AnyValue.ValueOneofCase.BoolValue, BoolValue: true } &&
               logRecord.Attributes.SingleOrDefault(value => value.Key == "Ratio")?.Value is { ValueCase: AnyValue.ValueOneofCase.DoubleValue, DoubleValue: 1.5 } &&
               VerifyStringAttribute(logRecord, "When", "01/02/2026 03:04:05") &&
               logRecord.Attributes.All(value => value.Key != "SourceContext") &&
               logRecord.Attributes.Count == 5;
    }

    private static bool VerifyStructuredAttributes(LogRecord logRecord)
    {
        return logRecord.Attributes.SingleOrDefault(value => value.Key == "Point")?.Value is { ValueCase: AnyValue.ValueOneofCase.KvlistValue } point &&
               point.KvlistValue.Values.Count == 2 &&
               point.KvlistValue.Values.SingleOrDefault(value => value.Key == "X")?.Value is { ValueCase: AnyValue.ValueOneofCase.IntValue, IntValue: 1 } &&
               point.KvlistValue.Values.SingleOrDefault(value => value.Key == "Label")?.Value is { ValueCase: AnyValue.ValueOneofCase.StringValue, StringValue: "origin" } &&
               logRecord.Attributes.SingleOrDefault(value => value.Key == "Items")?.Value is { ValueCase: AnyValue.ValueOneofCase.ArrayValue } items &&
               items.ArrayValue.Values.All(value => value.ValueCase == AnyValue.ValueOneofCase.IntValue) &&
               items.ArrayValue.Values.Select(value => value.IntValue).SequenceEqual([1L, 2L, 3L]);
    }

    private static bool VerifyNestedStructuredAttributes(LogRecord logRecord)
    {
        // The SDK writes structures inside an array, and key/value lists nested deeper than 3, as strings.
        return logRecord.Attributes.SingleOrDefault(value => value.Key == "Points")?.Value is { ValueCase: AnyValue.ValueOneofCase.ArrayValue } points &&
               points.ArrayValue.Values.All(value => value.ValueCase == AnyValue.ValueOneofCase.StringValue) &&
               points.ArrayValue.Values.Select(value => value.StringValue).SequenceEqual(["{ X: 1 }", "{ X: 2 }"]) &&
               logRecord.Attributes.SingleOrDefault(value => value.Key == "Levels")?.Value is { ValueCase: AnyValue.ValueOneofCase.KvlistValue } levels &&
               levels.KvlistValue.Values.SingleOrDefault(value => value.Key == "L2")?.Value is { ValueCase: AnyValue.ValueOneofCase.KvlistValue } level2 &&
               level2.KvlistValue.Values.SingleOrDefault(value => value.Key == "L3")?.Value is { ValueCase: AnyValue.ValueOneofCase.KvlistValue } level3 &&
               level3.KvlistValue.Values.SingleOrDefault(value => value.Key == "L4")?.Value is { ValueCase: AnyValue.ValueOneofCase.StringValue, StringValue: "{ X: 1 }" };
    }

    private static bool VerifyStringAttribute(LogRecord logRecord, string key, string expectedValue)
    {
        return logRecord.Attributes.SingleOrDefault(value => value.Key == key)?.Value is { ValueCase: AnyValue.ValueOneofCase.StringValue } value &&
               value.StringValue == expectedValue;
    }

    private static bool VerifyTraceContext(LogRecord logRecord)
    {
        return logRecord.TraceId != ByteString.Empty &&
               logRecord.SpanId != ByteString.Empty &&
               logRecord.Flags != 0;
    }

    private static bool VerifyBody(LogRecord logRecord, string expectedBody)
    {
        return Convert.ToString(logRecord.Body, CultureInfo.InvariantCulture) == $"{{ \"stringValue\": \"{expectedBody}\" }}";
    }

    private static bool VerifyExceptionAttributes(LogRecord logRecord)
    {
        // The SDK records Exception.GetType().Name, not the fully-qualified name.
        return logRecord.Attributes.Count(value => value.Key == "exception.stacktrace") == 1 &&
               VerifyStringAttribute(logRecord, "exception.message", "Example exception for testing") &&
               VerifyStringAttribute(logRecord, "exception.type", "InvalidOperationException");
    }
}
