using System;
using System.Collections.Generic;
using OpenTracing.Util;

namespace OpenTracingLibrary
{
    public static class Wrapper
    {
        public static void WithOpenTracingSpan(string spanKind, Action action)
        {
            var tracer = GlobalTracer.Instance;
            Console.WriteLine($">>>>>>>>>>>>>>>>>>>>>>> OpenTracing.{tracer}");
            using (var scope = tracer.BuildSpan("OpenTracing Span")
                .WithTag("span.kind", spanKind)
                .StartActive())
            {
                try
                {
                    action();
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
}
