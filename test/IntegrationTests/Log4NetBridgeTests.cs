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
    public void SubmitLogs(string packageVersion)
    {
        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);

        // Logged in scope of an activity
        collector.Expect(logRecord =>
            Convert.ToString(logRecord.Body) == "{ \"stringValue\": \"Hello, world!\" }" &&
            logRecord.TraceId != ByteString.Empty &&
            logRecord is { SeverityText: "INFO", SeverityNumber: SeverityNumber.Info } &&
            logRecord.Attributes.Count == 0);

        // Logged with exception
        collector.Expect(logRecord =>
            Convert.ToString(logRecord.Body) == "{ \"stringValue\": \"Exception occured\" }" &&
            logRecord.Attributes.Count == 3 &&
            logRecord is { SeverityText: "ERROR", SeverityNumber: SeverityNumber.Error } &&
            logRecord.Attributes.Count(value => value.Key == "exception.stacktrace") == 1 &&
            logRecord.Attributes.Count(value => value.Key == "exception.message") == 1 &&
            logRecord.Attributes.Count(value => value.Key == "exception.type") == 1);

        EnableBytecodeInstrumentation();

        var (standardOutput, _, _) = RunTestApplication(new()
        {
            PackageVersion = packageVersion,
        });

        standardOutput.Should().Contain("INFO  TestApplication.Log4NetBridge.Program - Hello, world!");
        standardOutput.Should().Contain("ERROR TestApplication.Log4NetBridge.Program - Exception occured");

        collector.AssertExpectations();
    }
}
