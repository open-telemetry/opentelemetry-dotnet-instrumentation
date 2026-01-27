// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Greet;
using Grpc.Core;

namespace TestApplication.GrpcNetClient;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes. This class is instantiated by app builder.
internal sealed class GreeterService : Greeter.GreeterBase
#pragma warning restore CA1812 // Avoid uninstantiated internal classes. This class is instantiated by app builder.
{
    public override async Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        var headers = new Metadata
        {
            { "Custom-Response-Test-Header1", "Test-Value1" },
            { "Custom-Response-Test-Header2", "Test-Value2" },
            { "Custom-Response-Test-Header3", "Test-Value3" }
        };

        await context.WriteResponseHeadersAsync(headers).ConfigureAwait(false);

        return new HelloReply
        {
            Message = "Hello " + request.Name
        };
    }
}
