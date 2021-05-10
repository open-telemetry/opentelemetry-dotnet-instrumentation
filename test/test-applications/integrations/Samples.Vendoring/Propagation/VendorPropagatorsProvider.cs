using System;
using System.Collections.Generic;
using Datadog.Trace.Conventions;
using Datadog.Trace.Propagation;
using Samples.Vendoring.Types;

namespace Samples.Vendoring.Propagation
{
    public class VendorPropagatorsProvider : IPropagatorsProvider
    {
        private static readonly IReadOnlyDictionary<string, Func<ITraceIdConvention, IPropagator>> PropagatorSelector =
            new Dictionary<string, Func<ITraceIdConvention, IPropagator>>(StringComparer.InvariantCultureIgnoreCase)
            {
                { VendorPropagatorTypes.VendorPropagator, convention => new VendorSpanContextPropagator(convention) }
            };

        public bool CanProvide(string propagatorId, ITraceIdConvention traceIdConvention)
        {
            return PropagatorSelector.ContainsKey(propagatorId);
        }

        public IPropagator GetPropagator(string propagatorId, ITraceIdConvention traceIdConvention)
        {
            if (PropagatorSelector.TryGetValue(propagatorId, out Func<ITraceIdConvention, IPropagator> getter))
            {
                return getter(traceIdConvention);
            }

            throw new InvalidOperationException($"There is no propagator registered for type '{propagatorId}'.");
        }
    }
}
