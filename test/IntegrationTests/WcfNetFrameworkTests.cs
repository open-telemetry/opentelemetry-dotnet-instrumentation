// <copyright file="WcfNetFrameworkTests.cs" company="OpenTelemetry Authors">
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

#if NETFRAMEWORK
using Google.Protobuf;
using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Trace.V1;
using Xunit.Abstractions;

namespace IntegrationTests;

public class WcfNetFrameworkTests : WcfTestsBase
{
    public WcfNetFrameworkTests(ITestOutputHelper output)
        : base("Wcf.Client.NetFramework", output)
    {
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task SubmitsTraces()
    {
        using var collector = await SubmitsTracesInternal(string.Empty);
        // TODO: better assertions
        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Kind == Span.Types.SpanKind.Server && span.ParentSpanId != ByteString.Empty, "Server 1");
        collector.Expect("OpenTelemetry.AutoInstrumentation.Wcf", span => span.Kind == Span.Types.SpanKind.Client, "Client 1");
        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Kind == Span.Types.SpanKind.Server && span.ParentSpanId != ByteString.Empty, "Server 2");
        collector.Expect("OpenTelemetry.AutoInstrumentation.Wcf", span => span.Kind == Span.Types.SpanKind.Client, "Client 2");
        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Kind == Span.Types.SpanKind.Server && span.ParentSpanId != ByteString.Empty, "Server 3");
        collector.Expect("OpenTelemetry.AutoInstrumentation.Wcf", span => span.Kind == Span.Types.SpanKind.Client, "Client 3");
        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Kind == Span.Types.SpanKind.Server && span.ParentSpanId != ByteString.Empty, "Server 4");
        collector.Expect("OpenTelemetry.AutoInstrumentation.Wcf", span => span.Kind == Span.Types.SpanKind.Client, "Client 4");
        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Kind == Span.Types.SpanKind.Server && span.ParentSpanId != ByteString.Empty, "Server 5");
        collector.Expect("OpenTelemetry.AutoInstrumentation.Wcf", span => span.Kind == Span.Types.SpanKind.Client, "Client 5");
        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Kind == Span.Types.SpanKind.Server && span.ParentSpanId != ByteString.Empty, "Server 6");
        collector.Expect("OpenTelemetry.AutoInstrumentation.Wcf", span => span.Kind == Span.Types.SpanKind.Client, "Client 6");

        collector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitsTracesNoEndpoint()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        collector.Expect("OpenTelemetry.AutoInstrumentation.Wcf", span => span.Kind == Span.Types.SpanKind.Client && span.Status.Code == Status.Types.StatusCode.Error, "Client 1");
        collector.Expect("OpenTelemetry.AutoInstrumentation.Wcf", span => span.Kind == Span.Types.SpanKind.Client && span.Status.Code == Status.Types.StatusCode.Error, "Client 2");
        collector.Expect("OpenTelemetry.AutoInstrumentation.Wcf", span => span.Kind == Span.Types.SpanKind.Client && span.Status.Code == Status.Types.StatusCode.Error, "Client 3");

        RunTestApplication(new TestSettings
        {
            PackageVersion = string.Empty
        });

        collector.AssertExpectations();
    }
}

#endif
