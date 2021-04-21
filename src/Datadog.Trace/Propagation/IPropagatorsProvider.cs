using Datadog.Trace.Conventions;

namespace Datadog.Trace.Propagation
{
    /// <summary>
    /// Factory interface for propagators.
    /// </summary>
    public interface IPropagatorsProvider
    {
        /// <summary>
        /// Gets propagator for propagator id.
        /// </summary>
        /// <param name="propagatorId">Propagator id.</param>
        /// <param name="traceIdConvention">Trace id convention.</param>
        /// <returns>Context propagator.</returns>
        IPropagator GetPropagator(string propagatorId, ITraceIdConvention traceIdConvention);

        /// <summary>
        /// Gets if provider can provide propagator for required spec.
        /// </summary>
        /// <param name="propagatorId">Propagator id.</param>
        /// <param name="traceIdConvention">Trace id convention.</param>
        /// <returns>Is providable.</returns>
        bool CanProvide(string propagatorId, ITraceIdConvention traceIdConvention);
    }
}
