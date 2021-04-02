using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Datadog.Trace.Conventions;
using Datadog.Trace.Logging;

namespace Datadog.Trace.Propagation
{
    internal class W3CSpanContextPropagator : IPropagator
    {
        private const string TraceParentFormat = "00-{0}-{1}-01";

        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<W3CSpanContextPropagator>();
        private static readonly int VersionPrefixIdLength = "00-".Length;
        private static readonly int TraceIdLength = "0af7651916cd43dd8448eb211c80319c".Length;
        private static readonly int VersionAndTraceIdLength = "00-0af7651916cd43dd8448eb211c80319c-".Length;
        private static readonly int SpanIdLength = "00f067aa0ba902b7".Length;

        private readonly ITraceIdConvention _traceIdConvention;

        public W3CSpanContextPropagator(ITraceIdConvention traceIdConvention)
        {
            _traceIdConvention = traceIdConvention;
        }

        public void Inject<T>(SpanContext context, T carrier, Action<T, string, string> setter)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (carrier == null)
            {
                throw new ArgumentNullException(nameof(carrier));
            }

            if (setter == null)
            {
                throw new ArgumentNullException(nameof(setter));
            }

            // lock sampling priority when span propagates.
            context.TraceContext?.LockSamplingPriority();

            setter(carrier, W3CHeaderNames.TraceParent, string.Format(TraceParentFormat, context.TraceId.ToString(), context.SpanId.ToString("x16")));
            if (!string.IsNullOrEmpty(context.TraceState))
            {
                setter(carrier, W3CHeaderNames.TraceState, context.TraceState);
            }
        }

        public SpanContext Extract<T>(T carrier, Func<T, string, IEnumerable<string>> getter)
        {
            if (carrier == null)
            {
                throw new ArgumentNullException(nameof(carrier));
            }

            if (getter == null)
            {
                throw new ArgumentNullException(nameof(getter));
            }

            var traceState = getter(carrier, W3CHeaderNames.TraceState).FirstOrDefault();

            var traceParentValues = getter(carrier, W3CHeaderNames.TraceParent).ToList();
            if (traceParentValues.Count != 1)
            {
                Log.Warning("Header {HeaderName} needs exactly 1 value", W3CHeaderNames.TraceParent);
                return null;
            }

            var traceParentHeader = traceParentValues[index: 0];
            var traceIdString = traceParentHeader.Substring(VersionPrefixIdLength, TraceIdLength);
            var traceId = _traceIdConvention.CreateFromString(traceIdString);
            if (traceId == TraceId.Zero)
            {
                Log.Warning("Could not parse {HeaderName} headers: {HeaderValues}", W3CHeaderNames.TraceParent, string.Join(",", traceParentValues));
                return null;
            }

            var spanIdString = traceParentHeader.Substring(VersionAndTraceIdLength, SpanIdLength);
            if (ulong.TryParse(spanIdString, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var spanId))
            {
                return spanId == 0 ? null : new SpanContext(traceId, spanId, traceState);
            }

            Log.Warning("Could not parse {HeaderName} headers: {HeaderValues}", W3CHeaderNames.TraceParent, string.Join(",", traceParentValues));
            return null;
        }
    }
}
