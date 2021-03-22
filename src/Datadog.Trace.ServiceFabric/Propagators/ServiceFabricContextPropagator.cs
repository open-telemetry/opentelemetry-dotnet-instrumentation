using Microsoft.ServiceFabric.Services.Remoting.V2;

namespace Datadog.Trace.ServiceFabric.Propagators
{
    internal abstract class ServiceFabricContextPropagator
    {
        public abstract void InjectContext(PropagationContext context, IServiceRemotingRequestMessageHeader messageHeaders);

        public abstract PropagationContext? ExtractContext(IServiceRemotingRequestMessageHeader messageHeaders);
    }
}
