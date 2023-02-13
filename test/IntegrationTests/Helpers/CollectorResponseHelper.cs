// <copyright file="CollectorResponseHelper.cs" company="OpenTelemetry Authors">
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

using System.Net;
#if NETFRAMEWORK
using System.Text;
#endif
using Google.Protobuf;

#if NET6_0_OR_GREATER
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

#if NET6_0_OR_GREATER
    public static async Task GenerateEmptyProtobufResponseAsync<T>(this HttpContext ctx)
        where T : IMessage, new()
    {
        ctx.Response.ContentType = "application/x-protobuf";
        ctx.Response.StatusCode = (int)HttpStatusCode.OK;
        var responseMessage = new T();
        ctx.Response.ContentLength = responseMessage.CalculateSize();

        using var outMemory = new MemoryStream();
        responseMessage.WriteTo(outMemory);

        await ctx.Response.Body.WriteAsync(outMemory.GetBuffer(), 0, (int)outMemory.Length);
        await ctx.Response.CompleteAsync();
    }

    public static async Task GenerateEmptyJsonResponseAsync(this HttpContext ctx)
    {
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync("{}");
    }
#endif
}
