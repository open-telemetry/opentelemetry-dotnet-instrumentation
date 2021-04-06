using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.Configuration;
using Datadog.Trace.Conventions;

namespace Datadog.Trace.Propagation
{
    internal static class ContextPropagatorFactory
    {
        private static readonly IReadOnlyDictionary<PropagatorType, Func<ITraceIdConvention, IPropagator>> PropagatorSelector =
            new Dictionary<PropagatorType, Func<ITraceIdConvention, IPropagator>>()
            {
                { PropagatorType.W3C, convention => new W3CSpanContextPropagator(convention) },
                { PropagatorType.B3, convention => new B3SpanContextPropagator(convention) },
                { PropagatorType.Datadog, convention => new DDSpanContextPropagator(convention) },
                { PropagatorType.Default, convention => new DDSpanContextPropagator(convention) },
            };

        public static ICollection<IPropagator> BuildPropagators(IEnumerable<PropagatorType> propagatorTypes, ITraceIdConvention traceIdConvention)
        {
            return propagatorTypes.Select(type => BuildPropagator(type, traceIdConvention)).ToList();
        }

        private static IPropagator BuildPropagator(PropagatorType propagatorType, ITraceIdConvention traceIdConvention)
        {
            if (PropagatorSelector.TryGetValue(propagatorType, out Func<ITraceIdConvention, IPropagator> getter))
            {
                // W3C propagator requires Otel TraceId convention as it's specification clearly states lengths of traceId and spanId values in the header.
                if (propagatorType == PropagatorType.W3C && traceIdConvention is not OtelTraceIdConvention)
                {
                    throw new NotSupportedException($"'{PropagatorType.W3C}' propagator requires '{ConventionType.OpenTelemetry}' convention to be set");
                }

                return getter(traceIdConvention);
            }

            throw new InvalidOperationException($"There is no propagator registered for type '{propagatorType}'");
        }
    }
}
