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
        private List<WebServerSpanExpectation> _expectations = new List<WebServerSpanExpectation>();

        public StartupHookTests(ITestOutputHelper output)
            : base("StartupHook", output)
        {
            SetServiceVersion("1.0.0");
            _expectations.Add(new WebServerSpanExpectation("Samples.StartupHook", null, "SayHello", "SayHello", null));
            _expectations.Add(new WebServerSpanExpectation("Samples.StartupHook", null, "HTTP GET", "HTTP GET", null, "GET"));
        }

        [Theory]
        [Trait("Category", "EndToEnd")]
        [InlineData(false)]
        [InlineData(true)]
        public void SubmitsTraces(bool enableCallTarget)
        {
            SetCallTargetSettings(enableCallTarget);

            int agentPort = TcpPortProvider.GetOpenPort();

            using (var agent = new MockZipkinCollector(Output, agentPort))
            using (var processResult = RunSampleAndWaitForExit(agent.Port, startupHook: true))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode} and exception: {processResult.StandardError}");
                var span = agent.WaitForSpans(2, 500);

                Assert.True(span.Count() == 2, $"Expecting 2 spans, received {span.Count()}");
                Assert.Single(span.Select(s => s.Service).Distinct());

                var spanList = span.ToList();
                this.AssertExpectationsMet(_expectations, spanList);
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
