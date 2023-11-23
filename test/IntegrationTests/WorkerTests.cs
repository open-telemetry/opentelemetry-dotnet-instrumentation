// <copyright file="WorkerTests.cs" company="OpenTelemetry Authors">
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

using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Logs.V1;
using Xunit.Abstractions;

#if NET6_0_OR_GREATER

namespace IntegrationTests;

public class WorkerTests : TestHelper
{
    public WorkerTests(ITestOutputHelper output)
        : base("Worker", output)
    {
    }

    [Fact]
    public async Task SubmitsLogsWithoutDuplicates()
    {
        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);

        collector.ExpectCollected(ValidateSingleAppLogRecord, "App log record should be exported once.");

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE", "true");

        EnableBytecodeInstrumentation();

        RunTestApplication();

        // wait for fixed amount of time for logs to be collected before asserting
        await Task.Delay(TimeSpan.FromSeconds(10));

        collector.AssertCollected();
    }

    private static bool ValidateSingleAppLogRecord(IEnumerable<LogRecord> records)
    {
        return records.Count(lr => Convert.ToString(lr.Body) == "{ \"stringValue\": \"Worker running.\" }") == 1;
    }
}

#endif
