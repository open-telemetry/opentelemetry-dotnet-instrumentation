using System;
using System.Collections.Generic;

namespace Datadog.Trace.Propagation
{
    internal class MultiplexSpanContextPropagator : IPropagator
    {
        private readonly ICollection<IPropagator> _propagators;

        public MultiplexSpanContextPropagator(ICollection<IPropagator> propagators)
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
