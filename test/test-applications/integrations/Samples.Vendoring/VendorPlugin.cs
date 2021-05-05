using Datadog.Trace.Plugins;
using Datadog.Trace.Propagation;
using Samples.Vendoring.Propagation;

namespace Samples.Vendoring
{
    public class VendorPlugin : IExtendPropagators
    {
        private IPropagatorsProvider _propagatorsProvider;

        public VendorPlugin()
        {
            _propagatorsProvider = new VendorPropagatorsProvider();
        }

        public IPropagatorsProvider GetPropagatorsProvider()
        {
            return _propagatorsProvider;
        }
    }
}
