// <copyright file="CustomSdkTests.cs" company="OpenTelemetry Authors">
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
using Google.Protobuf;
using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Trace.V1;
using Xunit.Abstractions;

namespace IntegrationTests;

[Collection(RedisCollection.Name)]
public class CustomSdkTests : TestHelper
{
    private readonly RedisFixture _redis;

    public CustomSdkTests(ITestOutputHelper output, RedisFixture redis)
        : base("CustomSdk", output)
    {
        _redis = redis;
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    public void SubmitsTraces_CustomSdk()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        // ensure spans are exported by custom sdk with custom resource
        collector.ResourceExpector.Expect("test_attr", "added_manually");
        collector.Expect("OpenTelemetry.Instrumentation.StackExchangeRedis", span => !IsTopLevel(span));

#if NET7_0_OR_GREATER
        collector.Expect("System.Net.Http", span => !IsTopLevel(span));
#else
        collector.Expect("OpenTelemetry.Instrumentation.Http.HttpClient", span => !IsTopLevel(span));
#endif
        collector.Expect("TestApplication.CustomSdk", span => IsTopLevel(span));

        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_INJECT_SDK", "false");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf");

        RunTestApplication(new()
        {
            Arguments = $"--redis {_redis.Port}"
        });

        collector.AssertExpectations();
        collector.ResourceExpector.AssertExpectations();
    }

    private static bool IsTopLevel(Span span)
    {
        return span.ParentSpanId == ByteString.Empty;
    }
}
#endif
