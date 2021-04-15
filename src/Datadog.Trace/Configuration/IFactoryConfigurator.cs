using Datadog.Trace.Configuration.Factories;

namespace Datadog.Trace.Configuration
{
    /// <summary>
    /// Provides configuration for factories
    /// </summary>
    public interface IFactoryConfigurator
    {
        /// <summary>
        /// Get the propagator factory.
        /// </summary>
        /// <returns>Propagator factory.</returns>
        IPropagatorFactory GetPropagatorFactory();
    }
}
