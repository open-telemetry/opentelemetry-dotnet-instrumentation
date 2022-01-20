using System;
using System.Collections.Generic;
using System.Linq;
using IntegrationTests.Helpers;
using IntegrationTests.Helpers.Mocks;
using IntegrationTests.Helpers.Models;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests.StartupHook
{
    public class StartupHookTests : TestHelper
    {
        private const string ServiceName = "Samples.MongoDB";

        private List<WebServerSpanExpectation> _expectations = new List<WebServerSpanExpectation>();

        public StartupHookTests(ITestOutputHelper output)
            : base("StartupHook", output)
        {
            SetEnvironmentVariable("OTEL_DOTNET_TRACER_INSTRUMENTATIONS", "HttpClient");
            _expectations.Add(new WebServerSpanExpectation(ServiceName, null, "SayHello", "SayHello", null));
            _expectations.Add(new WebServerSpanExpectation(ServiceName, null, "HTTP GET", "HTTP GET", null, "GET"));
        }

        [Fact]
        [Trait("Category", "EndToEnd")]
        public void SubmitsTraces()
        {
            SetEnvironmentVariable("OTEL_SERVICE_NAME", ServiceName);

            RunTestAndValidateResults(2);
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
                applicationNameToExclude = EnvironmentHelper.FullSampleName;
                expectedSpanCount = 0;
            }

            SetEnvironmentVariable("OTEL_SERVICE_NAME", ServiceName);
            SetEnvironmentVariable("OTEL_PROFILER_EXCLUDE_PROCESSES", $"dotnet,dotnet.exe,{applicationNameToExclude}");

            RunTestAndValidateResults(expectedSpanCount);
        }

        private void RunTestAndValidateResults(int expectedSpanCount)
        {
            int agentPort = TcpPortProvider.GetOpenPort();

            using (var agent = new MockZipkinCollector(Output, agentPort))
            using (var processResult = RunSampleAndWaitForExit(agent.Port))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode} and exception: {processResult.StandardError}");
                var span = agent.WaitForSpans(2, 500);

                Assert.True(span.Count() == expectedSpanCount, $"Expecting {expectedSpanCount} spans, received {span.Count()}");
                if (expectedSpanCount > 0)
                {
                    Assert.Single(span.Select(s => s.Service).Distinct());

                    var spanList = span.ToList();
                    AssertExpectationsMet(_expectations, spanList);
                }
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
}
