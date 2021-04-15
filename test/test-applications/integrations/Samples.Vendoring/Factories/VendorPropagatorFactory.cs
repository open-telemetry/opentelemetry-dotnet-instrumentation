using System;
using System.Collections.Generic;
using Datadog.Trace.Conventions;
using Datadog.Trace.Factories;
using Datadog.Trace.Propagation;
using Samples.Vendoring.Propagators;
using Samples.Vendoring.Types;

namespace Samples.Vendoring.Factories
{
    public class VendorPropagatorFactory : DefaultPropagatorFactory
    {
        public override IEnumerable<IPropagator> GetPropagators(IEnumerable<string> propagatorIds, ITraceIdConvention traceIdConvention)
        {
            foreach (string id in propagatorIds)
            {
                if (id.Equals(VendorPropagatorTypes.VendorPropagator, StringComparison.InvariantCultureIgnoreCase))
                {
                    yield return new VendorSpanContextPropagator(traceIdConvention);
                }
                else
                {
                    yield return base.GetPropagator(id, traceIdConvention);
                }
            }
        }
    }
}
