using System;
using System.Collections.Generic;

namespace Datadog.Trace.Propagation
{
    internal static class PropagationHelpers
    {
        public static string ParseString<T>(T carrier, Func<T, string, IEnumerable<string>> getter, string headerName)
        {
            var headerValues = getter(carrier, headerName);

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
