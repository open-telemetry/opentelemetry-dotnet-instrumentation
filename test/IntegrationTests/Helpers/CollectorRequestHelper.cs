// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using Microsoft.AspNetCore.Http;

namespace IntegrationTests.Helpers;

internal static class CollectorRequestHelper
{
    public static async Task<MemoryStream> ReadBodyToMemoryAsync(this HttpContext ctx)
    {
        if (!ctx.Request.Body.CanSeek)
        {
            // We only do this if the stream isn't *already* seekable,
            // as EnableBuffering will create a new stream instance
            // each time it's called
            ctx.Request.EnableBuffering();
        }

        ctx.Request.Body.Position = 0;

        var inMemory = new MemoryStream();
        await ctx.Request.Body.CopyToAsync(inMemory).ConfigureAwait(false);

        inMemory.Position = 0;

        return inMemory;
    }
}

#endif
