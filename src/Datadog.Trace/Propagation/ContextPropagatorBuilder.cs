using System;
using System.Collections.Generic;
using Datadog.Trace.Configuration;

namespace Datadog.Trace.Propagation
{
    internal static class ContextPropagatorBuilder
    {
        private static readonly IReadOnlyDictionary<PropagatorType, Func<IPropagator>> _propagatorSelector = new Dictionary<PropagatorType, Func<IPropagator>>()
        {
            { PropagatorType.B3, () => new B3SpanContextPropagator() },
            { PropagatorType.Datadog, () => new DDSpanContextPropagator() },
            { PropagatorType.Default, () => new DDSpanContextPropagator() },
        };

        public static IPropagator BuildPropagator(PropagatorType propagator)
        {
            if (_propagatorSelector.TryGetValue(propagator, out Func<IPropagator> getter))
            {
                return getter();
            }

            throw new InvalidOperationException($"There is no propagator registered for type '{propagator}'");
        }
    }
}
