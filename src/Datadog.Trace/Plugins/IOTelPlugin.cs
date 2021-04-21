using Datadog.Trace.Propagation;

namespace Datadog.Trace.Plugins
{
    /// <summary>
    /// Provides extendibility points for tracer
    /// </summary>
    public interface IOTelPlugin
    {
        /// <summary>
        /// Get the propagator provider.
        /// </summary>
        /// <returns>Propagator factory.</returns>
        IPropagatorsProvider GetPropagatorsProvider();
    }
}
