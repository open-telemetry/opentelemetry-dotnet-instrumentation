using Datadog.Trace.Configuration;
using Datadog.Trace.Configuration.Factories;
using Samples.Vendoring.Factories;

namespace Samples.Vendoring
{
    public class VendorFactoryConfigurator : DefaultFactoryConfigurator
    {
        private VendorPropagatorFactory _vendorPropagatorFactory;

        public override IPropagatorFactory GetPropagatorFactory()
        {
            return _vendorPropagatorFactory ??= new VendorPropagatorFactory();
        }
    }
}
