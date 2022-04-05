// <copyright file="HttpTests.cs" company="OpenTelemetry Authors">
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

using System.Linq;
using FluentAssertions.Execution;
using IntegrationTests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests.Http
{
    [Collection(HttpCollection.Name)]
    public class HttpTests : TestHelper
    {
        private const string ServiceName = "Samples.Http";

        public HttpTests(ITestOutputHelper output)
            : base("Http", output)
        {
            SetEnvironmentVariable("OTEL_SERVICE_NAME", ServiceName);
            SetEnvironmentVariable("OTEL_DOTNET_AUTO_ENABLED_INSTRUMENTATIONS", "HttpClient,AspNet");
        }

        [Fact]
        [Trait("Category", "EndToEnd")]
        public void SubmitTraces()
        {
            var agentPort = TcpPortProvider.GetOpenPort();
            using var agent = new MockZipkinCollector(Output, agentPort);

            const int expectedSpanCount = 3;

            using var processResult = RunSampleAndWaitForExit(agent.Port, enableStartupHook: true);
            Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode} and exception: {processResult.StandardError}");
            var spans = agent.WaitForSpans(expectedSpanCount, 500);

            using var scope = new AssertionScope();
            Assert.True(spans.Count == expectedSpanCount, $"Expecting {expectedSpanCount} spans, received {spans.Count}");

            var httpClientSpan = spans.FirstOrDefault(span => span.Name.Equals("HTTP GET"));
            var httpServerSpan = spans.FirstOrDefault(span => span.Name.Equals("/test"));
            var manualSpan = spans.FirstOrDefault(span => span.Name.Equals("manual span"));

            Assert.NotNull(httpClientSpan);
            Assert.NotNull(httpServerSpan);
            Assert.NotNull(manualSpan);

            Assert.False(httpClientSpan.ParentId.HasValue);
            Assert.Equal(httpClientSpan.SpanId, httpServerSpan.ParentId);
            Assert.Equal(httpServerSpan.SpanId, manualSpan.ParentId);

            Assert.Equal(ServiceName, httpClientSpan.Service);
            Assert.Equal(ServiceName, httpServerSpan.Service);
            Assert.Equal(ServiceName, manualSpan.Service);

            var httpClientTags = httpClientSpan.Tags;
            var httpServerTags = httpServerSpan.Tags;

            Assert.Equal(8, httpClientTags.Count);
            Assert.Equal("GET", httpClientTags["http.method"]);
            Assert.Equal(httpServerTags["http.host"], httpClientTags["http.host"]);
            Assert.Equal(httpServerTags["http.url"], httpClientTags["http.url"]);
            Assert.Equal("200", httpClientTags["http.status_code"]);
            Assert.Equal(httpServerTags["http.host"], httpClientTags["peer.service"]);
            Assert.Equal("client", httpClientTags["span.kind"]);
            Assert.Equal("server", httpServerTags["span.kind"]);
        }
    }
}
