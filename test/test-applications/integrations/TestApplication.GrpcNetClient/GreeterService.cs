// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Greet;
using Grpc.Core;

namespace TestApplication.GrpcNetClient;

public class GreeterService : Greeter.GreeterBase
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
