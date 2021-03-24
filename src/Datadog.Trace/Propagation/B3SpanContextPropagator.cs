using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Datadog.Trace.Headers;
using Datadog.Trace.Logging;

namespace Datadog.Trace.Propagation
{
    /// <summary>
    /// Class that hanbles B3 style context propagation.
    /// </summary>
    internal class B3SpanContextPropagator : Propagator
    {
        private const NumberStyles NumberStyle = NumberStyles.HexNumber;
        private static readonly string UserKeep = ((int)SamplingPriority.UserKeep).ToString(CultureInfo.InvariantCulture);
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<B3SpanContextPropagator>();

        private B3SpanContextPropagator()
        {
        }

        /// <summary>
        /// Gets the singleton instance of the propagator.
        /// </summary>
        public static B3SpanContextPropagator Instance { get; } = new B3SpanContextPropagator();

        /// <summary>
        /// Propagates the specified context by adding new headers to a <see cref="IHeadersCollection"/>.
        /// This locks the sampling priority for <paramref name="context"/>.
        /// </summary>
        /// <param name="context">A <see cref="SpanContext"/> value that will be propagated into <paramref name="headers"/>.</param>
        /// <param name="headers">A <see cref="IHeadersCollection"/> to add new headers to.</param>
        public override void Inject(SpanContext context, IHeadersCollection headers)
        {
            if (context == null) { throw new ArgumentNullException(nameof(context)); }

            if (headers == null) { throw new ArgumentNullException(nameof(headers)); }

            // lock sampling priority when span propagates.
            context.TraceContext?.LockSamplingPriority();

            headers.Set(B3HttpHeaderNames.B3TraceId, context.TraceId.ToString("x16", InvariantCulture));
            headers.Set(B3HttpHeaderNames.B3SpanId, context.SpanId.ToString("x16", InvariantCulture));

            if (context.ParentId != null)
            {
                headers.Set(B3HttpHeaderNames.B3ParentId, context.ParentId?.ToString("x16", InvariantCulture));
            }

            var samplingHeader = GetSamplingHeader(context);
            if (samplingHeader.HasValue)
            {
                headers.Set(samplingHeader.Value.Key, samplingHeader.Value.Value);
            }
        }

        public override void Inject<T>(SpanContext context, T carrier, Action<T, string, string> setter)
        {
            if (context == null) { throw new ArgumentNullException(nameof(context)); }

            if (carrier == null) { throw new ArgumentNullException(nameof(carrier)); }

            if (setter == null) { throw new ArgumentNullException(nameof(setter)); }

            // lock sampling priority when span propagates.
            context.TraceContext?.LockSamplingPriority();

            setter(carrier, B3HttpHeaderNames.B3TraceId, context.TraceId.ToString("x16", InvariantCulture));
            setter(carrier, B3HttpHeaderNames.B3SpanId, context.SpanId.ToString("x16", InvariantCulture));

            if (context.ParentId != null)
            {
                setter(carrier, B3HttpHeaderNames.B3ParentId, context.ParentId?.ToString("x16", InvariantCulture));
            }

            var samplingHeader = GetSamplingHeader(context);
            if (samplingHeader.HasValue)
            {
                setter(carrier, samplingHeader.Value.Key, samplingHeader.Value.Value);
            }
        }

        /// <summary>
        /// Extracts a <see cref="SpanContext"/> from the values found in the specified headers.
        /// </summary>
        /// <param name="headers">The headers that contain the values to be extracted.</param>
        /// <returns>A new <see cref="SpanContext"/> that contains the values obtained from <paramref name="headers"/>.</returns>
        public override SpanContext Extract(IHeadersCollection headers)
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            var traceId = ParseHexUInt64(headers, B3HttpHeaderNames.B3TraceId);

            if (traceId == 0)
            {
                // a valid traceId is required to use distributed tracing
                return null;
            }

            var spanId = ParseHexUInt64(headers, B3HttpHeaderNames.B3SpanId);
            var samplingPriority = ParseB3Sampling(headers);

            return new SpanContext(traceId, spanId, samplingPriority);
        }

        public override SpanContext Extract<T>(T carrier, Func<T, string, IEnumerable<string>> getter)
        {
            if (carrier == null) { throw new ArgumentNullException(nameof(carrier)); }

            if (getter == null) { throw new ArgumentNullException(nameof(getter)); }

            var traceId = ParseHexUInt64(carrier, getter, B3HttpHeaderNames.B3TraceId);

            if (traceId == 0)
            {
                // a valid traceId is required to use distributed tracing
                return null;
            }

            var spanId = ParseHexUInt64(carrier, getter, B3HttpHeaderNames.B3SpanId);
            var samplingPriority = ParseB3Sampling(carrier, getter);

            return new SpanContext(traceId, spanId, samplingPriority);
        }

        private static ulong ParseHexUInt64(IHeadersCollection headers, string headerName)
        {
            var headerValues = headers.GetValues(headerName).ToList();

            return ParseHexUnit64(headerValues, headerName);
        }

        private static ulong ParseHexUInt64<T>(T carrier, Func<T, string, IEnumerable<string>> getter, string headerName)
        {
            var headerValues = getter(carrier, headerName);

            return ParseHexUnit64(headerValues, headerName);
        }

        private static ulong ParseHexUnit64(IEnumerable<string> headerValues, string headerName)
        {
            if (headerValues.Any())
            {
                foreach (var headerValue in headerValues)
                {
                    if (ulong.TryParse(headerValue, NumberStyle, InvariantCulture, out var result))
                    {
                        return result;
                    }
                }

                Log.Information("Could not parse {0} headers: {1}", headerName, string.Join(",", headerValues));
            }

            return 0;
        }

        private static SamplingPriority? ParseB3Sampling(IHeadersCollection headers)
        {
            var debugged = headers.GetValues(B3HttpHeaderNames.B3Flags);
            var sampled = headers.GetValues(B3HttpHeaderNames.B3Sampled);

            return ParseB3Sampling(debugged, sampled);
        }

        private static SamplingPriority? ParseB3Sampling<T>(T carrier, Func<T, string, IEnumerable<string>> getter)
        {
            var debugged = getter(carrier, B3HttpHeaderNames.B3Flags);
            var sampled = getter(carrier, B3HttpHeaderNames.B3Sampled);

            return ParseB3Sampling(debugged, sampled);
        }

        private static SamplingPriority? ParseB3Sampling(IEnumerable<string> debuggedValues, IEnumerable<string> sampledValues)
        {
            var debugged = debuggedValues.ToList();
            var sampled = sampledValues.ToList();

            if (debugged.Count != 0 && (debugged[0] == "0" || debugged[0] == "1"))
            {
                return debugged[0] == "1" ? SamplingPriority.UserKeep : null;
            }
            else if (sampled.Count != 0 && (sampled[0] == "0" || sampled[0] == "1"))
            {
                return sampled[0] == "1" ? SamplingPriority.AutoKeep : SamplingPriority.AutoReject;
            }

            return null;
        }

        private KeyValuePair<string, string>? GetSamplingHeader(SpanContext context)
        {
            var samplingPriority = (int?)(context.TraceContext?.SamplingPriority ?? context.SamplingPriority);
            if (samplingPriority != null)
            {
                var value = samplingPriority < (int)SamplingPriority.AutoKeep ? "0" : "1";
                var header = samplingPriority == (int)SamplingPriority.UserKeep ? B3HttpHeaderNames.B3Flags : B3HttpHeaderNames.B3Sampled;

                return new KeyValuePair<string, string>(header, value);
            }

            return null;
        }
    }
}
