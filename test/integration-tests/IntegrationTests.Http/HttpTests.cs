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

using System;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using IntegrationTests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests.Http
{
    public class HttpTests : TestHelper
    {
        private const string ServiceName = "TestApplication.Http";

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

            using var processResult = RunTestApplicationAndWaitForExit(agent.Port, enableStartupHook: true);
            Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode} and exception: {processResult.StandardError}");
            var spans = agent.WaitForSpans(expectedSpanCount, TimeSpan.FromSeconds(5));

            using (new AssertionScope())
            {
                spans.Count.Should().Be(expectedSpanCount);

                // ASP.NET Core auto-instrumentation is generating spans
                var httpClientSpan = spans.FirstOrDefault(span => span.Name.Equals("HTTP GET"));
                var httpServerSpan = spans.FirstOrDefault(span => span.Name.Equals("/test"));
                var manualSpan = spans.FirstOrDefault(span => span.Name.Equals("manual span"));

                httpClientSpan.Should().NotBeNull();
                httpServerSpan.Should().NotBeNull();
                manualSpan.Should().NotBeNull();

                // checking trace hierarchy
                httpClientSpan.ParentId.HasValue.Should().BeFalse();
                httpServerSpan.ParentId.Should().Be(httpClientSpan.SpanId);
                manualSpan.ParentId.Should().Be(httpServerSpan.SpanId);

                httpClientSpan.Service.Should().Be(ServiceName);
                httpServerSpan.Service.Should().Be(ServiceName);
                manualSpan.Service.Should().Be(ServiceName);

                var httpClientTags = httpClientSpan.Tags;
                var httpServerTags = httpServerSpan.Tags;

                httpClientTags.Count.Should().Be(8);
                httpClientTags["http.method"].Should().Be("GET");
                httpClientTags["http.host"].Should().Be(httpServerTags["http.host"]);
                httpClientTags["http.url"].Should().Be(httpServerTags["http.url"]);
                httpClientTags["http.status_code"].Should().Be("200");
                httpClientTags["peer.service"].Should().Be(httpServerTags["http.host"]);
                httpClientTags["span.kind"].Should().Be("client");
                httpServerTags["span.kind"].Should().Be("server");
            }
        }
    }
}
