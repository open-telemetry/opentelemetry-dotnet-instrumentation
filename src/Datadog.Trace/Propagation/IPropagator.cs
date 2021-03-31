using System;
using System.Collections.Generic;

namespace Datadog.Trace.Propagation
{
    internal interface IPropagator
    {
        /// <summary>
        /// Propagates the specified context by adding new headers to a carrier
        /// This locks the sampling priority for <paramref name="context"/>.
        /// </summary>
        /// <param name="context">A <see cref="SpanContext"/> value that will be propagated into <paramref name="carrier"/> instance.</param>
        /// <param name="carrier">The headers metadata carrier to add to.</param>
        /// <param name="setter">The action that can set a header in the carrier.</param>
        /// <typeparam name="T">Type of the headers metadata carrier</typeparam>
        void Inject<T>(SpanContext context, T carrier, Action<T, string, string> setter);

        /// <summary>
        /// Extracts a <see cref="SpanContext"/> from the values found in the specified headers carrier.
        /// </summary>
        /// <param name="carrier">The headers metadata carrier to extract data from.</param>
        /// <param name="getter">The function that can extract a list of values for a given header name.</param>
        /// <typeparam name="T">Type of the headers metadata carrier</typeparam>
        /// <returns>A new <see cref="SpanContext"/> that contains the values obtained from <paramref name="carrier"/> instance.</returns>
        SpanContext Extract<T>(T carrier, Func<T, string, IEnumerable<string>> getter);
    }
}
