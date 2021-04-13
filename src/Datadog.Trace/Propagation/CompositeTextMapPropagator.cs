using System;
using System.Collections.Generic;

namespace Datadog.Trace.Propagation
{
    /// <summary>
    /// Composite Propagator.
    /// See more details <see href="https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/context/api-propagators.md#composite-propagator">here</see>.
    /// </summary>
    internal class CompositeTextMapPropagator : IPropagator
    {
        private readonly ICollection<IPropagator> _propagators;

        public CompositeTextMapPropagator(ICollection<IPropagator> propagators)
        {
            _propagators = propagators;
        }

        public SpanContext Extract<T>(T carrier, Func<T, string, IEnumerable<string>> getter)
        {
            foreach (IPropagator propagator in _propagators)
            {
                SpanContext context = propagator.Extract(carrier, getter);

                // if context is missing, propagator wasn't capable to extract
                if (context != null)
                {
                    return context;
                }
            }

            // non of the propagators found specific headers
            return null;
        }

        public void Inject<T>(SpanContext context, T carrier, Action<T, string, string> setter)
        {
            foreach (IPropagator propagator in _propagators)
            {
                propagator.Inject(context, carrier, setter);
            }
        }
    }
}
