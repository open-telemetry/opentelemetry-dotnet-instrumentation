// <copyright file="MassTransitTests.cs" company="OpenTelemetry Authors">
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

#if !NETFRAMEWORK
using System.Threading.Tasks;
using IntegrationTests.Helpers;
using Xunit;
using Xunit.Abstractions;
using static OpenTelemetry.Proto.Trace.V1.Span.Types;

namespace IntegrationTests;

public class MassTransitTests : TestHelper
{
    public MassTransitTests(ITestOutputHelper output)
        : base("MassTransit", output)
    {
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task SubmitsTraces()
    {
        using var collector = await MockSpansCollector.Start(Output);
        collector.Expect("MassTransit", span => span.Kind == SpanKind.Producer, "Producer");
        collector.Expect("MassTransit", span => span.Kind == SpanKind.Consumer, "Consumer");

        RunTestApplication(otlpTraceCollectorPort: collector.Port);

        collector.AssertExpectations();
    }
}
#endif
