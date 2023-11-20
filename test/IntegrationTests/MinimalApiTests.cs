// <copyright file="MinimalApiTests.cs" company="OpenTelemetry Authors">
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

#if NET6_0_OR_GREATER

using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Logs.V1;
using Xunit.Abstractions;

namespace IntegrationTests;

public class MinimalApiTests : TestHelper
{
    public MinimalApiTests(ITestOutputHelper output)
        : base("MinimalApi", output)
    {
    }

    [SkippableTheory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [Trait("Category", "EndToEnd")]
    public async Task SubmitsLogsWithoutDuplicates(bool enableByteCodeInstrumentation, bool enableHostingStartupAssembly)
    {
#if NET6_0
        Skip.If(EnvironmentTools.IsMacOS(), "Known issue on MacOS.");
#endif

        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE", "true");

        if (enableByteCodeInstrumentation)
        {
            EnableBytecodeInstrumentation();
            collector.ExpectCollected(ValidateCombinedLogsExport, "All log records should be exported once.");
        }

        if (enableHostingStartupAssembly)
        {
            SetEnvironmentVariable("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES", "OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper");
            if (!enableByteCodeInstrumentation)
            {
                // otherwise expectation was already set
                collector.ExpectCollected(ValidateSingleAppLogExport, "Application log records should be exported once.");
            }
        }

        RunTestApplication();

        // wait for fixed amount of time for logs to be collected before asserting
        await Task.Delay(TimeSpan.FromSeconds(10));

        collector.AssertCollected();
    }

    private static bool ValidateCombinedLogsExport(IEnumerable<LogRecord> records)
    {
        return ValidateSingleAppLogExport(records) && ValidateSingleBeforeHostLogRecord(records);
    }

    private static bool ValidateSingleBeforeHostLogRecord(IEnumerable<LogRecord> records)
    {
        var beforeHostLogCount = records.Count(lr => Convert.ToString(lr.Body) == "{ \"stringValue\": \"Logged before host is built.\" }");
        return beforeHostLogCount == 1;
    }

    private static bool ValidateSingleAppLogExport(IEnumerable<LogRecord> records)
    {
        var appLogCount = records.Count(lr => Convert.ToString(lr.Body) == "{ \"stringValue\": \"Request received.\" }");
        return appLogCount == 1;
    }
}
#endif
