// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

public class GrpcNetClientTests : TestHelper
{
    public GrpcNetClientTests(ITestOutputHelper output)
        : base("GrpcNetClient", output)
    {
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [MemberData(nameof(LibraryVersion.GrpcNetClient), MemberType = typeof(LibraryVersion))]
    public async Task SubmitsTraces(string packageVersion)
    {
        using var collector = await MockSpansCollector.InitializeAsync(Output);
        SetExporter(collector);
        collector.Expect("OpenTelemetry.Instrumentation.GrpcNetClient");

        // Grpc.Net.Client is using various version of http communication under the hood.
        // Enabling only GrpcNetClient instrumentation to have consistent set of spans.
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_INSTRUMENTATION_ENABLED", "false");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_GRPCNETCLIENT_INSTRUMENTATION_ENABLED", "true");
        RunTestApplication(new TestSettings { PackageVersion = packageVersion });

        collector.AssertExpectations();
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [MemberData(nameof(LibraryVersion.GrpcNetClient), MemberType = typeof(LibraryVersion))]
    public async Task SubmitTracesCapturesGrpcMetadata(string packageVersion)
    {
        using var collector = await MockSpansCollector.InitializeAsync(Output);
        SetExporter(collector);

        collector.Expect("OpenTelemetry.Instrumentation.GrpcNetClient", span =>
        {
            return span.Attributes.Any(x => x.Key == "rpc.grpc.request.metadata.custom-request-test-header1" && x.Value.StringValue == "Test-Value1")
                   && span.Attributes.Any(x => x.Key == "rpc.grpc.request.metadata.custom-request-test-header3" && x.Value.StringValue == "Test-Value3")
                   && span.Attributes.All(x => x.Key != "rpc.grpc.request.metadata.custom-request-test-header2")
#if NETFRAMEWORK
                   ; // there is no .NET Framework server
#else
                   && span.Attributes.Any(x => x.Key == "rpc.grpc.response.metadata.custom-response-test-header2" && x.Value.StringValue == "Test-Value2")
                   && span.Attributes.All(x => x.Key != "rpc.grpc.response.metadata.custom-response-test-header1")
                   && span.Attributes.All(x => x.Key != "rpc.grpc.response.metadata.custom-response-test-header3");
#endif
        });

        // Grpc.Net.Client is using various version of http communication under the hood.
        // Enabling only GrpcNetClient instrumentation to have consistent set of spans.
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_INSTRUMENTATION_ENABLED", "false");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_GRPCNETCLIENT_INSTRUMENTATION_ENABLED", "true");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_GRPCNETCLIENT_INSTRUMENTATION_CAPTURE_REQUEST_METADATA", "Custom-Request-Test-Header1,Custom-Request-Test-Header3");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_GRPCNETCLIENT_INSTRUMENTATION_CAPTURE_RESPONSE_METADATA", "Custom-Response-Test-Header2");

        RunTestApplication(new TestSettings { PackageVersion = packageVersion });

        collector.AssertExpectations();
    }
}
