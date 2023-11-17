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

#if NET6_0_OR_GREATER
using FluentAssertions;
using FluentAssertions.Execution;
using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Trace.V1;
using Xunit.Abstractions;

namespace IntegrationTests;

public class HttpTests : TestHelper
{
    public HttpTests(ITestOutputHelper output)
        : base("Http", output)
    {
    }

    [Theory]
    [InlineData("")] // equivalent of default value
    [InlineData("b3multi")]
    [InlineData("b3")]
    [Trait("Category", "EndToEnd")]
    public void SubmitTraces(string propagators)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        Span? clientSpan = null;
#if NET7_0_OR_GREATER
        collector.Expect("System.Net.Http", span =>
#else
        collector.Expect("OpenTelemetry.Instrumentation.Http.HttpClient", span =>
#endif
        {
            clientSpan = span;
            return true;
        });
        Span? serverSpan = null;
        collector.Expect("OpenTelemetry.Instrumentation.AspNetCore", span =>
        {
            serverSpan = span;
            return true;
        });
        Span? manualSpan = null;
        collector.Expect("TestApplication.Http", span =>
        {
            manualSpan = span;
            return true;
        });

        SetEnvironmentVariable("OTEL_PROPAGATORS", propagators);
        SetEnvironmentVariable("DISABLE_DistributedContextPropagator", "true");
        RunTestApplication();

        collector.AssertExpectations();
        using (new AssertionScope())
        {
            // testing context propagation via trace hierarchy
            clientSpan!.ParentSpanId.IsEmpty.Should().BeTrue();
            serverSpan!.ParentSpanId.Should().Equal(clientSpan.SpanId);
            manualSpan!.ParentSpanId.Should().Equal(serverSpan.SpanId);
        }
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitMetrics()
    {
        using var collector = new MockMetricsCollector(Output);
        SetExporter(collector);
        collector.Expect("OpenTelemetry.Instrumentation.AspNetCore");
#if NET8_0_OR_GREATER
        collector.Expect("System.Net.Http");
        collector.Expect("System.Net.NameResolution");
        collector.ExpectAdditionalEntries(x => x.All(m => m.InstrumentationScopeName != "OpenTelemetry.Instrumentation.Http"));
#else
        collector.Expect("OpenTelemetry.Instrumentation.Http");
#endif

        RunTestApplication();

        collector.AssertExpectations();
    }
}
#endif
