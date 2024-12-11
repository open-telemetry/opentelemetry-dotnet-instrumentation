// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using FluentAssertions;
using Google.Protobuf;
using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Logs.V1;
using Xunit.Abstractions;

namespace IntegrationTests;

public class Log4NetBridgeTests : TestHelper
{
    public Log4NetBridgeTests(ITestOutputHelper output)
        : base("Log4NetBridge", output)
    {
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [MemberData(nameof(LibraryVersion.log4net), MemberType = typeof(LibraryVersion))]
    public void SubmitLogs_ThroughLog4NetBridge_WhenLog4NetIsUsedDirectlyForLogging(string packageVersion)
    {
        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);

        // Logged in scope of an activity
        collector.Expect(logRecord =>
            VerifyBody(logRecord, "Hello, world!") &&
            VerifyTraceContext(logRecord) &&
            logRecord is { SeverityText: "INFO", SeverityNumber: SeverityNumber.Info } &&
            logRecord.Attributes.Count == 0);

        // Logged with exception
        collector.Expect(logRecord =>
            VerifyBody(logRecord, "Exception occured") &&
            VerifyTraceContext(logRecord) &&
            logRecord is { SeverityText: "ERROR", SeverityNumber: SeverityNumber.Error } &&
            VerifyExceptionAttributes(logRecord) &&
            logRecord.Attributes.Count == 3);

        EnableBytecodeInstrumentation();

        var (standardOutput, _, _) = RunTestApplication(new()
        {
            PackageVersion = packageVersion,
            Arguments = "--api log4net"
        });

        AssertStandardOutputExpectations(standardOutput);

        collector.AssertExpectations();
    }

#if !NETFRAMEWORK
    [Theory]
    [Trait("Category", "EndToEnd")]
    [MemberData(nameof(LibraryVersion.log4net), MemberType = typeof(LibraryVersion))]
    public void SubmitLogs_ThroughILoggerBridge_WhenLog4NetIsUsedAsILoggerAppenderForLogging(string packageVersion)
    {
        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);

        // Logged in scope of an activity
        collector.Expect(logRecord =>
            VerifyBody(logRecord, "Hello, {0}!") &&
            VerifyTraceContext(logRecord) &&
            logRecord is { SeverityText: "Information", SeverityNumber: SeverityNumber.Info } &&
            // ILogger bridge adds original format when using structured logging.
            logRecord.Attributes.Count == 1);

        // Logged with exception
        collector.Expect(logRecord =>
            VerifyBody(logRecord, "Exception occured") &&
            VerifyTraceContext(logRecord) &&
            // OtlpLogExporter adds exception related attributes (ConsoleExporter doesn't show them)
            logRecord is { SeverityText: "Error", SeverityNumber: SeverityNumber.Error } &&
            VerifyExceptionAttributes(logRecord) &&
            logRecord.Attributes.Count == 3);

        EnableBytecodeInstrumentation();

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
    [MemberData(nameof(LibraryVersion.log4net), MemberType = typeof(LibraryVersion))]
    public async Task SubmitLogs_ThroughILoggerBridge_WhenLog4NetIsUsedAsILoggerAppenderForLogging_WithoutDuplicates(string packageVersion)
    {
        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);

        collector.ExpectCollected(records => records.Count == 2, "App logs should be exported once.");

        EnableBytecodeInstrumentation();

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

    private static bool VerifyTraceContext(LogRecord logRecord)
    {
        return logRecord.TraceId != ByteString.Empty &&
               logRecord.SpanId != ByteString.Empty &&
               logRecord.Flags != 0;
    }

    private static void AssertStandardOutputExpectations(string standardOutput)
    {
        standardOutput.Should().Contain("INFO  TestApplication.Log4NetBridge.Program - Hello, world!");
        standardOutput.Should().Contain("ERROR TestApplication.Log4NetBridge.Program - Exception occured");
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
