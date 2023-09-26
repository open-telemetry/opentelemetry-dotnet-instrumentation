// <copyright file="LogRazorTests.cs" company="OpenTelemetry Authors">
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
using Xunit.Abstractions;

namespace IntegrationTests;

public class LogRazorTests : TestHelper
{
    public LogRazorTests(ITestOutputHelper output)
        : base("Razor", output)
    {
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Trait("Category", "EndToEnd")]
    public void SubmitLogs(bool enableClrProfiler)
    {
        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);
        collector.Expect(logRecord =>
        {
            var logsAsString = Convert.ToString(logRecord);
            return logsAsString != null && logsAsString.Contains("Warning from Razor App.");
        });

        if (enableClrProfiler)
        {
            EnableBytecodeInstrumentation();
        }
        else
        {
            SetEnvironmentVariable("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES", "OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper");
        }

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE", "true");
        RunTestApplication();

        collector.AssertExpectations();
    }
}
#endif
