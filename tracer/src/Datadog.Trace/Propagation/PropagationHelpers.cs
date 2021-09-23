using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.Conventions;
using Datadog.Trace.Logging;

namespace Datadog.Trace.Propagation
{
    internal static class PropagationHelpers
    {
        public static TraceId ParseTraceId<T>(T carrier, Func<T, string, IEnumerable<string>> getter, string headerName, ITraceIdConvention traceIdConvention, IDatadogLogger logger)
        {
            var headerValues = getter(carrier, headerName) ?? Enumerable.Empty<string>();

            foreach (var headerValue in headerValues)
            {
                var traceId = traceIdConvention.CreateFromString(headerValue);
                if (traceId == TraceId.Zero)
                {
                    continue;
                }

                return traceId;
            }

            logger.Warning("Could not parse {HeaderName} headers: {HeaderValues}", headerName, string.Join(",", headerValues));
            return TraceId.Zero;
        }

        public static string ParseString<T>(T carrier, Func<T, string, IEnumerable<string>> getter, string headerName)
        {
            var headerValues = getter(carrier, headerName) ?? Enumerable.Empty<string>();

            foreach (var headerValue in headerValues)
            {
                if (!string.IsNullOrEmpty(headerValue))
                {
                    return headerValue;
                }
            }

            return null;
        }
    }
}
