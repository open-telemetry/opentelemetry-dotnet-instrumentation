// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Net;
using System.Net.Http;

namespace OpenTelemetry.AutoInstrumentation.Tests.OpAmp.Fixtures;

internal sealed class OpAmpTestHttpMessageHandler(Func<byte[]>? responseContentFactory = null) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var content = responseContentFactory?.Invoke() ?? [];
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(content),
        });
    }
}
