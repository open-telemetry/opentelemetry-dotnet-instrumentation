using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Datadog.Trace.Headers;
using Datadog.Trace.Logging;

namespace Datadog.Trace.Propagation
{
    /// <summary>
    /// Class that handles Datadog style context propagation.
    /// </summary>
    internal class DDSpanContextPropagator : IPropagator
    {
        private const NumberStyles NumberStyles = System.Globalization.NumberStyles.Integer;
        private const int MinimumSamplingPriority = (int)SamplingPriority.UserReject;
        private const int MaximumSamplingPriority = (int)SamplingPriority.UserKeep;

        private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<DDSpanContextPropagator>();

        private static readonly int[] SamplingPriorities;

        static DDSpanContextPropagator()
        {
            SamplingPriorities = Enum.GetValues(typeof(SamplingPriority)).Cast<int>().ToArray();
        }

        private DDSpanContextPropagator()
        {
        }

        public static DDSpanContextPropagator Instance { get; } = new DDSpanContextPropagator();

        /// <summary>
        /// Propagates the specified context by adding new headers to a <see cref="IHeadersCollection"/>.
        /// This locks the sampling priority for <paramref name="context"/>.
        /// </summary>
        /// <param name="context">A <see cref="SpanContext"/> value that will be propagated into <paramref name="carrier"/>.</param>
        /// <param name="carrier">The headers to add to.</param>
        /// <param name="setter">The action that can set a header in the carrier.</param>
        /// <typeparam name="T">Type of header collection</typeparam>
        public void Inject<T>(SpanContext context, T carrier, Action<T, string, string> setter)
        {
            if (context == null) { throw new ArgumentNullException(nameof(context)); }

            if (carrier == null) { throw new ArgumentNullException(nameof(carrier)); }

            if (setter == null) { throw new ArgumentNullException(nameof(setter)); }

            // lock sampling priority when span propagates.
            context.TraceContext?.LockSamplingPriority();

            setter(carrier, DDHttpHeaderNames.TraceId, context.TraceId.ToString(InvariantCulture));
            setter(carrier, DDHttpHeaderNames.ParentId, context.SpanId.ToString(InvariantCulture));

            // avoid writing origin header if not set, keeping the previous behavior.
            if (context.Origin != null)
            {
                setter(carrier, DDHttpHeaderNames.Origin, context.Origin);
            }

            var samplingPriority = (int?)(context.TraceContext?.SamplingPriority ?? context.SamplingPriority);

            setter(carrier, DDHttpHeaderNames.SamplingPriority, samplingPriority?.ToString(InvariantCulture));
        }

        /// <summary>
        /// Extracts a <see cref="SpanContext"/> from the values found in the specified headers.
        /// </summary>
        /// <param name="carrier">The headers that contain the values to be extracted.</param>
        /// <param name="getter">The function that can extract a list of values for a given header name.</param>
        /// <typeparam name="T">Type of header collection</typeparam>
        /// <returns>A new <see cref="SpanContext"/> that contains the values obtained from <paramref name="carrier"/>.</returns>
        public SpanContext Extract<T>(T carrier, Func<T, string, IEnumerable<string>> getter)
        {
            if (carrier == null) { throw new ArgumentNullException(nameof(carrier)); }

            if (getter == null) { throw new ArgumentNullException(nameof(getter)); }

            var traceId = ParseUInt64(carrier, getter, DDHttpHeaderNames.TraceId);

            if (traceId == 0)
            {
                // a valid traceId is required to use distributed tracing
                return null;
            }

            var parentId = ParseUInt64(carrier, getter, DDHttpHeaderNames.ParentId);
            var samplingPriority = ParseSamplingPriority(carrier, getter, DDHttpHeaderNames.SamplingPriority);
            var origin = PropagationHelpers.ParseString(carrier, getter, DDHttpHeaderNames.Origin);

            return new SpanContext(traceId, parentId, samplingPriority, null, origin);
        }

        private static ulong ParseUInt64<T>(T carrier, Func<T, string, IEnumerable<string>> getter, string headerName)
        {
            var headerValues = getter(carrier, headerName);
            var hasValue = false;

            foreach (var headerValue in headerValues)
            {
                if (ulong.TryParse(headerValue, NumberStyles, InvariantCulture, out var result))
                {
                    return result;
                }

                hasValue = true;
            }

            if (hasValue)
            {
                Log.Warning("Could not parse {HeaderName} headers: {HeaderValues}", headerName, string.Join(",", headerValues));
            }

            return 0;
        }

        private static SamplingPriority? ParseSamplingPriority<T>(T carrier, Func<T, string, IEnumerable<string>> getter, string headerName)
        {
            var headerValues = getter(carrier, headerName);

            var hasValue = false;

            foreach (var headerValue in headerValues)
            {
                if (int.TryParse(headerValue, out var result))
                {
                    if (MinimumSamplingPriority <= result && result <= MaximumSamplingPriority)
                    {
                        return (SamplingPriority)result;
                    }
                }

                hasValue = true;
            }

            if (hasValue)
            {
                Log.Warning(
                    "Could not parse {HeaderName} headers: {HeaderValues}",
                    headerName,
                    string.Join(",", headerValues));
            }

            return default;
        }
    }
}
