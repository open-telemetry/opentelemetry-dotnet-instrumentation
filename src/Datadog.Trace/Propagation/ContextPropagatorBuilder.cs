using System;
using System.Collections.Generic;
using Datadog.Trace.Configuration;
using Datadog.Trace.Conventions;

namespace Datadog.Trace.Propagation
{
    internal static class ContextPropagatorBuilder
    {
        private static readonly IReadOnlyDictionary<PropagatorType, Func<ITraceIdConvention, IPropagator>> _propagatorSelector =
            new Dictionary<PropagatorType, Func<ITraceIdConvention, IPropagator>>()
            {
                { PropagatorType.B3, (convention) => new B3SpanContextPropagator(convention) },
                { PropagatorType.Datadog, (convention) => new DDSpanContextPropagator(convention) },
                { PropagatorType.Default, (convention) => new DDSpanContextPropagator(convention) },
            };

        public static IPropagator BuildPropagator(PropagatorType propagator, ITraceIdConvention traceIdConvention)
        {
            if (_propagatorSelector.TryGetValue(propagator, out Func<ITraceIdConvention, IPropagator> getter))
            {
                return getter(traceIdConvention);
            }

            throw new InvalidOperationException($"There is no propagator registered for type '{propagator}'");
        }
    }
}
