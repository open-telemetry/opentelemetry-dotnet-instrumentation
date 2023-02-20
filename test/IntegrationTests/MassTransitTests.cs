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

#if NET6_0_OR_GREATER
using IntegrationTests.Helpers;
using Xunit.Abstractions;
using static OpenTelemetry.Proto.Trace.V1.Span.Types;

namespace IntegrationTests;

public class MassTransitTests : TestHelper
{
    public MassTransitTests(ITestOutputHelper output)
        : base("MassTransit", output)
    {
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [MemberData(nameof(TestPackageVersions.MassTransit), MemberType = typeof(TestPackageVersions))]
    public void SubmitsTraces(string packageVersion)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        collector.Expect("MassTransit", span => span.Kind == SpanKind.Producer, "Producer");
        collector.Expect("MassTransit", span => span.Kind == SpanKind.Consumer, "Consumer");

        RunTestApplication(new TestSettings { PackageVersion = packageVersion });

        collector.AssertExpectations();
    }
}
#endif
