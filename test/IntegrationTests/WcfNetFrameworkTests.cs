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
        await SubmitsTracesInternal(string.Empty);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitsTracesNoEndpoint()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Status.Code == Status.Types.StatusCode.Error, "Client 1");

        collector.Expect("TestApplication.Wcf.Client.NetFramework", span => span.Kind == Span.Types.SpanKind.Internal, "Custom parent");
        collector.Expect("TestApplication.Wcf.Client.NetFramework", span => span.Kind == Span.Types.SpanKind.Internal, "Custom sibling");

        collector.ExpectCollected(WcfClientInstrumentation.ValidateExpectedSpanHierarchy);

        RunTestApplication(new TestSettings
        {
            PackageVersion = string.Empty
        });

        collector.AssertExpectations();
    }
}

#endif
