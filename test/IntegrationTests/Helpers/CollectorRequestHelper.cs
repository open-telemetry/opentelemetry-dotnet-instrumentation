// <copyright file="CollectorRequestHelper.cs" company="OpenTelemetry Authors">
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

#if NETCOREAPP3_1_OR_GREATER
using System.IO;
using System.Threading.Tasks;
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
        await ctx.Request.Body.CopyToAsync(inMemory);

        inMemory.Position = 0;

        return inMemory;
    }
}

#endif
