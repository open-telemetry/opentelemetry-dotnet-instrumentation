using System.Collections.Generic;
using Datadog.Trace.Conventions;
using Datadog.Trace.Propagation;

namespace Datadog.Trace.Configuration.Factories
{
    /// <summary>
    /// Factory interface for propagators.
    /// </summary>
    public interface IPropagatorFactory
    {
        /// <summary>
        /// Gets multiple span context propagators.
        /// </summary>
        /// <param name="propagatorIds">List of propagator ids.</param>
        /// <param name="traceIdConvention">Trace id convention.</param>
        /// <returns>Context propagators.</returns>
        IEnumerable<IPropagator> GetPropagators(IEnumerable<string> propagatorIds, ITraceIdConvention traceIdConvention);

        /// <summary>
        /// Gets propagator for propagator id.
        /// </summary>
        /// <param name="propagatorId">Propagator id.</param>
        /// <param name="traceIdConvention">Trace id convention.</param>
        /// <returns>Context propagator.</returns>
        IPropagator GetPropagator(string propagatorId, ITraceIdConvention traceIdConvention);
    }
}
