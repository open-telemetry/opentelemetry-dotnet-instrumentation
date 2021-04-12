using System;
using System.Collections.Generic;
using Datadog.Trace.Configuration;
using Datadog.Trace.Conventions;

namespace Datadog.Trace.Propagation
{
    internal static class ContextPropagatorBuilder
    {
        private static readonly IReadOnlyDictionary<PropagatorType, Func<ITraceIdConvention, IPropagator>> PropagatorSelector =
            new Dictionary<PropagatorType, Func<ITraceIdConvention, IPropagator>>()
            {
                { PropagatorType.W3C, convention => new W3CSpanContextPropagator(convention) },
                { PropagatorType.B3, convention => new B3SpanContextPropagator(convention) },
                { PropagatorType.Datadog, convention => new DDSpanContextPropagator(convention) },
                { PropagatorType.Default, convention => new DDSpanContextPropagator(convention) },
            };

        public static IPropagator BuildPropagator(PropagatorType propagator, ITraceIdConvention traceIdConvention)
        {
            if (PropagatorSelector.TryGetValue(propagator, out Func<ITraceIdConvention, IPropagator> getter))
            {
                // W3C propagator requires Otel TraceId convention as it's specification clearly states lengths of traceId and spanId values in the header.
                if (propagator == PropagatorType.W3C && traceIdConvention is not OtelTraceIdConvention)
                {
                    throw new NotSupportedException($"'{PropagatorType.W3C}' propagator requires '{ConventionType.OpenTelemetry}' convention to be set");
                }

                return getter(traceIdConvention);
            }

            throw new InvalidOperationException($"There is no propagator registered for type '{propagator}'");
        }
    }
}
