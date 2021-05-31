// <copyright file="WebRequestTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Linq;
using Datadog.Core.Tools;
using Datadog.Trace.ClrProfiler.IntegrationTests.Helpers;
using Datadog.Trace.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    [CollectionDefinition(nameof(WebRequestTests), DisableParallelization = true)]
    [Collection(nameof(WebRequestTests))]
    public class WebRequestTests : TestHelper
    {
        public WebRequestTests(ITestOutputHelper output)
            : base("WebRequest", output)
        {
            SetServiceVersion("1.0.0");
            SetEnvironmentVariable("OTEL_PROPAGATORS", "datadog,b3");
        }

        [Theory]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        [InlineData(false)]
        [InlineData(true)]
        public void SubmitsTraces(bool enableCallTarget)
        {
            SetCallTargetSettings(enableCallTarget);

            int expectedSpanCount = EnvironmentHelper.IsCoreClr() ? 71 : 27; // .NET Framework automatic instrumentation doesn't cover Async / TaskAsync operations

            var ignoreAsync = EnvironmentHelper.IsCoreClr() ? string.Empty : "IgnoreAsync ";

            const string expectedOperationName = "http.request";
            const string expectedServiceName = "Samples.WebRequest-http-client";

            int agentPort = TcpPortProvider.GetOpenPort();
            int httpPort = TcpPortProvider.GetOpenPort();

            Output.WriteLine($"Assigning port {agentPort} for the agentPort.");
            Output.WriteLine($"Assigning port {httpPort} for the httpPort.");

            using (var agent = new MockTracerAgent(agentPort))
            using (ProcessResult processResult = RunSampleAndWaitForExit(agent.Port, arguments: $"{ignoreAsync}Port={httpPort}"))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode}");

                var spans = agent.WaitForSpans(expectedSpanCount, operationName: expectedOperationName);
                Assert.Equal(expectedSpanCount, spans.Count);

                foreach (var span in spans)
                {
                    Assert.Equal(expectedOperationName, span.Name);
                    Assert.Equal(expectedServiceName, span.Service);
                    Assert.Equal(SpanTypes.Http, span.Type);
                    Assert.True(string.Equals(span.Tags[Tags.InstrumentationName], "WebRequest") || string.Equals(span.Tags[Tags.InstrumentationName], "HttpMessageHandler"));
                    Assert.False(span.Tags?.ContainsKey(Tags.Version), "External service span should not have service version tag.");
                }

                PropagationTestHelpers.AssertPropagationEnabled(spans.First(), processResult);
            }
        }

        [Theory]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        [InlineData(false)]
        [InlineData(true)]
        public void TracingDisabled_DoesNotSubmitsTraces(bool enableCallTarget)
        {
            SetCallTargetSettings(enableCallTarget);

            const string expectedOperationName = "http.request";

            int agentPort = TcpPortProvider.GetOpenPort();
            int httpPort = TcpPortProvider.GetOpenPort();

            using (var agent = new MockTracerAgent(agentPort))
            using (ProcessResult processResult = RunSampleAndWaitForExit(agent.Port, arguments: $"TracingDisabled Port={httpPort}"))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode}");

                var spans = agent.WaitForSpans(1, 3000, operationName: expectedOperationName);
                Assert.Equal(0, spans.Count);

                PropagationTestHelpers.AssertPropagationDisabled(processResult);
            }
        }
    }
}
