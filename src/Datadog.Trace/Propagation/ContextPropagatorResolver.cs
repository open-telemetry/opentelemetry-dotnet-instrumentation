using System;
using System.Collections.Generic;
using Datadog.Trace.Configuration;

namespace Datadog.Trace.Propagation
{
    internal static class ContextPropagatorResolver
    {
        private static readonly IReadOnlyDictionary<PropagatorType, Func<IPropagator>> _propagatorSelector = new Dictionary<PropagatorType, Func<IPropagator>>()
        {
            { PropagatorType.B3, () => B3SpanContextPropagator.Instance },
            { PropagatorType.Datadog, () => DDSpanContextPropagator.Instance },
            { PropagatorType.Default, () => DDSpanContextPropagator.Instance },
        };

        public static IPropagator GetPropagator(PropagatorType propagator)
        {
            if (_propagatorSelector.TryGetValue(propagator, out Func<IPropagator> getter))
            {
                return getter();
            }

            throw new InvalidOperationException($"There is no propagator registered for type '{propagator}'");
        }
    }
}
