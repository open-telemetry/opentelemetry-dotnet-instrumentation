// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;
using Google.Protobuf;
using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Logs.V1;
using Xunit.Abstractions;

namespace IntegrationTests;

public class NLogBridgeTests : TestHelper
{
    public NLogBridgeTests(ITestOutputHelper output)
        : base("NLogBridge", output)
    {
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [MemberData(nameof(LibraryVersion.NLog), MemberType = typeof(LibraryVersion))]
    public void SubmitLogs_ThroughNLogBridge_WhenNLogIsUsedDirectlyForLogging(string packageVersion)
    {
        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);

        // Logged in scope of an activity
        collector.Expect(
            logRecord =>
            VerifyBody(logRecord, "{0}, {1} at {2:t}!") &&
            VerifyTraceContext(logRecord) &&
            logRecord is { SeverityText: "Info", SeverityNumber: SeverityNumber.Info } &&
            VerifyAttributes(logRecord) &&
            logRecord.Attributes.Count == 4,
            "Expected Info record.");

        // Logged with exception
        collector.Expect(
            logRecord =>
            VerifyBody(logRecord, "Exception occured") &&
            logRecord is { SeverityText: "Error", SeverityNumber: SeverityNumber.Error } &&
            VerifyExceptionAttributes(logRecord) &&
            logRecord.Attributes.Count == 4,
            "Expected Error record.");

        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_ENABLE_NLOG_BRIDGE", "true");

        var (standardOutput, _, _) = RunTestApplication(new()
        {
            PackageVersion = packageVersion,
            Arguments = "--api nlog"
        });

        AssertStandardOutputExpectations(standardOutput);

        collector.AssertExpectations();
    }

#if NET
    [Theory]
    [Trait("Category", "EndToEnd")]
    [MemberData(nameof(LibraryVersion.NLog), MemberType = typeof(LibraryVersion))]
    public void SubmitLogs_ThroughILoggerBridge_WhenNLogIsUsedAsILoggerProviderForLogging(string packageVersion)
    {
        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);

        // Logged in scope of an activity
        collector.Expect(
            logRecord =>
            VerifyBody(logRecord, "{0}, {1} at {2:t}!") &&
            VerifyTraceContext(logRecord) &&
            logRecord is { SeverityText: "Info", SeverityNumber: SeverityNumber.Info } &&
            // 0 : "Hello"
            // 1 : "world"
            // 2 : timestamp
            logRecord.Attributes.Count == 3,
            "Expected Info record.");

        // Logged with exception
        collector.Expect(
            logRecord =>
            VerifyBody(logRecord, "Exception occured") &&
            // OtlpLogExporter adds exception related attributes (ConsoleExporter doesn't show them)
            logRecord is { SeverityText: "Error", SeverityNumber: SeverityNumber.Error } &&
            VerifyExceptionAttributes(logRecord) &&
            logRecord.Attributes.Count == 3,
            "Expected Error record.");

        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_ENABLE_NLOG_BRIDGE", "true");

        var (standardOutput, _, _) = RunTestApplication(new()
        {
            PackageVersion = packageVersion,
            Arguments = "--api ILogger"
        });

        AssertStandardOutputExpectations(standardOutput);

        collector.AssertExpectations();
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [MemberData(nameof(LibraryVersion.NLog), MemberType = typeof(LibraryVersion))]
    public async Task SubmitLogs_ThroughILoggerBridge_WhenNLogIsUsedAsILoggerProviderForLogging_WithoutDuplicates(string packageVersion)
    {
        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);

        collector.ExpectCollected(records => records.Count == 3, "App logs should be exported once.");

        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_ENABLE_NLOG_BRIDGE", "true");

        var (standardOutput, _, _) = RunTestApplication(new()
        {
            PackageVersion = packageVersion,
            Arguments = "--api ILogger"
        });

        AssertStandardOutputExpectations(standardOutput);

        // wait for fixed amount of time for logs to be collected before asserting
        await Task.Delay(TimeSpan.FromSeconds(5));

        collector.AssertCollected();
    }

#endif

    [Theory]
    [Trait("Category", "EndToEnd")]
    [MemberData(nameof(LibraryVersion.NLog), MemberType = typeof(LibraryVersion))]
    public void TraceContext_IsInjectedIntoCurrentNLogLogsDestination(string packageVersion)
    {
        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_ENABLE_NLOG_BRIDGE", "false");

        var (standardOutput, _, _) = RunTestApplication(new()
        {
            PackageVersion = packageVersion,
            Arguments = "--api nlog"
        });

        var regex = new Regex(@"INFO  TestApplication\.NLogBridge\.Program - Hello, world at \d{2}\:\d{2}\! span_id=[a-f0-9]{16} trace_id=[a-f0-9]{32} trace_flags=01");
        var output = standardOutput;
        Assert.Matches(regex, output);
        Assert.Contains("ERROR TestApplication.NLogBridge.Program - Exception occured span_id=(null) trace_id=(null) trace_flags=(null)", output);
    }

    private static bool VerifyAttributes(LogRecord logRecord)
    {
        var firstArgAttribute = logRecord.Attributes.SingleOrDefault(value => value.Key == "0");
        var secondArgAttribute = logRecord.Attributes.SingleOrDefault(value => value.Key == "1");
        var customAttribute = logRecord.Attributes.SingleOrDefault(value => value.Key == "test_key");
        return firstArgAttribute?.Value.StringValue == "Hello" &&
               secondArgAttribute?.Value.StringValue == "world" &&
               logRecord.Attributes.Count(value => value.Key == "2") == 1 &&
               customAttribute?.Value.StringValue == "test_value";
    }

    private static bool VerifyTraceContext(LogRecord logRecord)
    {
        return logRecord.TraceId != ByteString.Empty &&
               logRecord.SpanId != ByteString.Empty &&
               logRecord.Flags != 0;
    }

    private static void AssertStandardOutputExpectations(string standardOutput)
    {
        Assert.Contains("INFO  TestApplication.NLogBridge.Program - Hello, world at", standardOutput);
        Assert.Contains("ERROR TestApplication.NLogBridge.Program - Exception occured", standardOutput);
    }

    private static bool VerifyBody(LogRecord logRecord, string expectedBody)
    {
        return Convert.ToString(logRecord.Body) == $"{{ \"stringValue\": \"{expectedBody}\" }}";
    }

    private static bool VerifyExceptionAttributes(LogRecord logRecord)
    {
        return logRecord.Attributes.Count(value => value.Key == "exception.stacktrace") == 1 &&
               logRecord.Attributes.Count(value => value.Key == "exception.message") == 1 &&
               logRecord.Attributes.Count(value => value.Key == "exception.type") == 1;
    }
}
