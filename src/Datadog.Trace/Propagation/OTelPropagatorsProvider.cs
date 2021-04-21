using System;
using System.Collections.Generic;
using Datadog.Trace.Configuration;
using Datadog.Trace.Configuration.Types;
using Datadog.Trace.Conventions;

namespace Datadog.Trace.Propagation
{
    internal class OTelPropagatorsProvider : IPropagatorsProvider
    {
        private static readonly IReadOnlyDictionary<string, Func<ITraceIdConvention, IPropagator>> PropagatorSelector =
            new Dictionary<string, Func<ITraceIdConvention, IPropagator>>(StringComparer.InvariantCultureIgnoreCase)
            {
                        { PropagatorTypes.W3C, convention => new W3CSpanContextPropagator(convention) },
                        { PropagatorTypes.B3, convention => new B3SpanContextPropagator(convention) },
                        { PropagatorTypes.Datadog, convention => new DDSpanContextPropagator(convention) },
            };

        public bool CanProvide(string propagatorId, ITraceIdConvention traceIdConvention)
        {
            if (propagatorId == PropagatorTypes.W3C && traceIdConvention is not OtelTraceIdConvention)
            {
                return false;
            }

            return PropagatorSelector.ContainsKey(propagatorId);
        }

        /// <summary>
        /// Builds the propagator with given spec.
        /// </summary>
        /// <param name="propagatorId">Propagator id.</param>
        /// <param name="traceIdConvention">Trace id convention.</param>
        /// <returns>Context propagator.</returns>
        public virtual IPropagator GetPropagator(string propagatorId, ITraceIdConvention traceIdConvention)
        {
            if (PropagatorSelector.TryGetValue(propagatorId, out Func<ITraceIdConvention, IPropagator> getter))
            {
                // W3C propagator requires Otel TraceId convention as it's specification clearly states lengths of traceId and spanId values in the header.
                if (propagatorId == PropagatorTypes.W3C && traceIdConvention is not OtelTraceIdConvention)
                {
                    throw new NotSupportedException($"'{PropagatorTypes.W3C}' propagator requires '{ConventionType.OpenTelemetry}' convention to be set");
                }

                return getter(traceIdConvention);
            }

            throw new InvalidOperationException($"There is no propagator registered for type '{propagatorId}'.");
        }
    }
}
