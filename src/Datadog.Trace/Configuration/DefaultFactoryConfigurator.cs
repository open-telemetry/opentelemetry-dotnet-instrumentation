using Datadog.Trace.Configuration.Factories;
using Datadog.Trace.Factories;

namespace Datadog.Trace.Configuration
{
    /// <summary>
    /// Default implementation for factories.
    /// </summary>
    public class DefaultFactoryConfigurator : IFactoryConfigurator
    {
        private IPropagatorFactory _propagatorFactory;

        /// <summary>
        /// Gets the default propagator factory.
        /// </summary>
        /// <returns>Default propagator factory.</returns>
        public virtual IPropagatorFactory GetPropagatorFactory()
        {
            return _propagatorFactory ??= new DefaultPropagatorFactory();
        }
    }
}
