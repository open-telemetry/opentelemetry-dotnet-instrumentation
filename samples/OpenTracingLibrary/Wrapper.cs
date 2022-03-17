// <copyright file="Wrapper.cs" company="OpenTelemetry Authors">
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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenTracing.Util;

namespace OpenTracingLibrary;

public static class Wrapper
{
    public static async Task WithOpenTracingSpanAsync(string spanKind, Func<Task> wrapped)
    {
        var tracer = GlobalTracer.Instance;
        Console.WriteLine($">>>>>>>>>>>>>>>>>>>>>>> OpenTracing.{tracer}");
        using (var scope = tracer.BuildSpan("OpenTracing Span")
                   .WithTag("span.kind", spanKind)
                   .StartActive())
        {
            try
            {
                await wrapped();
                scope.Span.Log("action success");
                scope.Span.SetTag("action.success", true);
            }
            catch (Exception ex)
            {
                scope.Span.SetTag("error", true);
                var eventData = new Dictionary<string, object>
                {
                    { "event", "error" },
                    { "error.kind", "Exception" },
                    { "error.object", ex },
                    { "stack", ex.StackTrace.ToString() },
                };
                scope.Span.Log(eventData);
                throw;
            }
        }
    }
}