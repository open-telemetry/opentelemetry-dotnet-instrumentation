using Datadog.Trace.Conventions;
using Datadog.Trace.Plugins;

namespace Datadog.Trace.Propagation
{
    /// <summary>
    /// Factory interface for propagators.
    /// Implementation must have a default (parameterless) constructor.
    /// </summary>
    public interface IPropagatorsProvider : IOTelExtension
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
