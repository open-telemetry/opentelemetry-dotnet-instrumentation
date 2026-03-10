// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Net;
#if NETFRAMEWORK
using System.Text;
#endif
using Google.Protobuf;

#if NET
using Microsoft.AspNetCore.Http;
#endif

namespace IntegrationTests.Helpers;

internal static class CollectorResponseHelper
{
#if NETFRAMEWORK
    public static void GenerateEmptyProtobufResponse<T>(this HttpListenerContext ctx)
        where T : IMessage, new()
    {
        // NOTE: HttpStreamRequest doesn't support Transfer-Encoding: Chunked
        // (Setting content-length avoids that)
        ctx.Response.ContentType = "application/x-protobuf";
        ctx.Response.StatusCode = (int)HttpStatusCode.OK;
        var responseMessage = new T();
        ctx.Response.ContentLength64 = responseMessage.CalculateSize();
        responseMessage.WriteTo(ctx.Response.OutputStream);
        ctx.Response.Close();
    }

    public static void GenerateEmptyJsonResponse(this HttpListenerContext ctx)
    {
        // NOTE: HttpStreamRequest doesn't support Transfer-Encoding: Chunked
        // (Setting content-length avoids that)
        ctx.Response.ContentType = "application/json";
        var buffer = Encoding.UTF8.GetBytes("{}");
        ctx.Response.ContentLength64 = buffer.LongLength;
        ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);
        ctx.Response.Close();
    }
#endif

#if NET
    public static async Task GenerateEmptyProtobufResponseAsync<T>(this HttpContext ctx)
        where T : IMessage, new()
    {
        ctx.Response.ContentType = "application/x-protobuf";
        ctx.Response.StatusCode = (int)HttpStatusCode.OK;
        var responseMessage = new T();
        ctx.Response.ContentLength = responseMessage.CalculateSize();

        using var outMemory = new MemoryStream();
        responseMessage.WriteTo(outMemory);

        await ctx.Response.Body.WriteAsync(outMemory.GetBuffer().AsMemory(0, (int)outMemory.Length)).ConfigureAwait(false);
        await ctx.Response.CompleteAsync().ConfigureAwait(false);
    }

    public static async Task GenerateEmptyJsonResponseAsync(this HttpContext ctx)
    {
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync("{}").ConfigureAwait(false);
    }
#endif
}
