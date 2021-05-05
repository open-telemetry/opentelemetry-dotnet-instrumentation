using Datadog.Trace.Propagation;

namespace Datadog.Trace.Plugins
{
    /// <summary>
    /// Provides propagators extendability point.
    /// </summary>
    public interface IExtendPropagators : IOTelExtension
    {
        /// <summary>
        /// Get the propagator provider.
        /// </summary>
        /// <returns>Propagator factory.</returns>
        IPropagatorsProvider GetPropagatorsProvider();
    }
}
