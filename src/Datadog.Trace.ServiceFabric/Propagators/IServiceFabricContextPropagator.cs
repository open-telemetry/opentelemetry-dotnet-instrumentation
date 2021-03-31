using Microsoft.ServiceFabric.Services.Remoting.V2;

namespace Datadog.Trace.ServiceFabric.Propagators
{
    internal interface IServiceFabricContextPropagator
    {
        abstract void InjectContext(PropagationContext context, IServiceRemotingRequestMessageHeader messageHeaders);

        abstract PropagationContext? ExtractContext(IServiceRemotingRequestMessageHeader messageHeaders);
    }
}
