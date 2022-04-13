// <copyright file="StartupHookTests.cs" company="OpenTelemetry Authors">
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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using IntegrationTests.Helpers;
using IntegrationTests.Helpers.Mocks;
using IntegrationTests.Helpers.Models;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests.StartupHook;

public class StartupHookTests : TestHelper
{
    private const string ServiceName = "TestApplication.MongoDB";

    private List<WebServerSpanExpectation> _expectations = new List<WebServerSpanExpectation>();

    public StartupHookTests(ITestOutputHelper output)
        : base("StartupHook", output)
    {
        SetEnvironmentVariable("OTEL_SERVICE_NAME", ServiceName);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_ENABLED_INSTRUMENTATIONS", "HttpClient");
        _expectations.Add(new WebServerSpanExpectation(ServiceName, null, "SayHello", "SayHello", null));
        _expectations.Add(new WebServerSpanExpectation(ServiceName, null, "HTTP GET", "HTTP GET", null, "GET"));
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitsTraces()
    {
        const int expectedSpanCount = 2;

        var spans = RunTestApplication(enableStartupHook: true);

        AssertExpectedSpansReceived(expectedSpanCount, spans);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void InstrumentationNotAvailableWhenStartupHookIsNotEnabled()
    {
        const int expectedSpanCount = 0;

        var spans = RunTestApplication(enableStartupHook: false);

        AssertExpectedSpansReceived(expectedSpanCount, spans);
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [InlineData(true)]
    [InlineData(false)]
    public void ApplicationIsExcluded(bool excludeApplication)
    {
        string applicationNameToExclude = string.Empty;
        int expectedSpanCount = 2;
        if (excludeApplication)
        {
            applicationNameToExclude = EnvironmentHelper.FullTestApplicationName;
            expectedSpanCount = 0;
        }

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES", $"dotnet,dotnet.exe,{applicationNameToExclude}");

        var spans = RunTestApplication(enableStartupHook: true);

        AssertExpectedSpansReceived(expectedSpanCount, spans);
    }

    private IImmutableList<IMockSpan> RunTestApplication(bool enableStartupHook)
    {
        int agentPort = TcpPortProvider.GetOpenPort();

        using (var agent = new MockZipkinCollector(Output, agentPort))
        using (var processResult = RunTestApplicationAndWaitForExit(agent.Port, enableStartupHook: enableStartupHook))
        {
            Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode} and exception: {processResult.StandardError}");
            return agent.WaitForSpans(2, 500);
        }
    }

    private void AssertExpectedSpansReceived(int expectedSpanCount, IImmutableList<IMockSpan> spans)
    {
        Assert.True(spans.Count() == expectedSpanCount, $"Expecting {expectedSpanCount} spans, received {spans.Count()}");
        if (expectedSpanCount > 0)
        {
            Assert.Single(spans.Select(s => s.Service).Distinct());

            var spanList = spans.ToList();
            AssertExpectationsMet(_expectations, spanList);
        }
    }

    private void AssertExpectationsMet(List<WebServerSpanExpectation> expectations, List<IMockSpan> spans)
    {
        List<IMockSpan> remainingSpans = spans.Select(s => s).ToList();
        List<string> failures = new List<string>();

        foreach (SpanExpectation expectation in expectations)
        {
            List<IMockSpan> possibleSpans =
                remainingSpans
                    .Where(s => expectation.Matches(s))
                    .ToList();

            if (possibleSpans.Count == 0)
            {
                failures.Add($"No spans for: {expectation}");
                continue;
            }
        }

        string finalMessage = Environment.NewLine + string.Join(Environment.NewLine, failures.Select(f => " - " + f));
        Assert.True(!failures.Any(), finalMessage);
    }
}
